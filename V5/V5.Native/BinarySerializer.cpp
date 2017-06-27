#include "stdafx.h"
#include "BinarySerializer.h"

namespace V5
{
	namespace Serialization
	{
		const int BufferLengthBytes = 32 * 1024;

		generic <typename T>
		int ElementSize()
		{
			if (T::typeid == Boolean::typeid) return 1;
			return System::Runtime::InteropServices::Marshal::SizeOf<T>();
		}

		generic <typename T>
		String^ BinarySerializer::TypeIdentifier()
		{
			if (T::typeid == Boolean::typeid) return "b1";
			if (T::typeid == Byte::typeid) return "b8";

			if (T::typeid == Int16::typeid) return "i16";
			if (T::typeid == Int32::typeid) return "i32";
			if (T::typeid == Int64::typeid) return "i64";

			if (T::typeid == UInt16::typeid) return "u16";
			if (T::typeid == UInt32::typeid) return "u32";
			if (T::typeid == UInt64::typeid) return "u64";

			if (T::typeid == Single::typeid) return "f32";
			if (T::typeid == Double::typeid) return "f64";

			throw gcnew NotImplementedException();
		}

		generic <typename T>
		void BinarySerializer::Write(String^ filePath, array<T>^ array)
		{
			Write(filePath, array, 0, array->Length);
		}

		generic <typename T>
		void BinarySerializer::Write(String^ filePath, array<T>^ array, int index, int length)
		{
			String^ fullPath = filePath + "." + TypeIdentifier<T>() + ".bin";
			String^ temporaryPath = Path::ChangeExtension(fullPath, ".new");

			// Ensure the containing folder exists
			String^ serializationDirectory = Path::GetDirectoryName(fullPath);
			if (!String::IsNullOrEmpty(serializationDirectory)) Directory::CreateDirectory(serializationDirectory);

			// Serialize the item
			__int64 lengthWritten = 0;
			FileStream^ s = gcnew FileStream(temporaryPath, FileMode::Create, FileAccess::Write, FileShare::Delete);
			try
			{
				BinaryWriter^ writer = gcnew BinaryWriter(s);
				Write<T>(writer, array, index, length);
				lengthWritten = s->Position;
			}
			finally
			{
				if (s != nullptr) delete s;
			}

			if (lengthWritten == 0)
			{
				// If nothing was written, delete the file
				File::Delete(temporaryPath);
				File::Delete(fullPath);
			}
			else
			{
				// Otherwise, replace the previous official file with the new one
				File::Delete(fullPath);
				File::Move(temporaryPath, fullPath);
			}
		}

		generic <typename T>
		array<T>^ BinarySerializer::Read(String^ filePath)
		{
			String^ fullPath = filePath + "." + TypeIdentifier<T>() + ".bin";
			if (!File::Exists(fullPath)) return gcnew array<T>(0);

			FileStream^ s = gcnew FileStream(fullPath, FileMode::Open, FileAccess::Read, FileShare::ReadWrite);
			try
			{
				BinaryReader^ reader = gcnew BinaryReader(s);
				return ReadArray<T>(reader, (int)s->Length);
			}
			finally
			{
				if (s != nullptr) delete s;
			}
		}

		generic <typename T>
		array<T>^ BinarySerializer::ReadArray(BinaryReader^ reader, __int64 lengthBytes)
		{
			if (lengthBytes == 0) return gcnew array<T>(0);

			int elementSize = ElementSize<T>();
			int arrayLength = (int)lengthBytes / elementSize;

			array<T>^ values = gcnew array<T>(arrayLength);

			// Read byte[] directly
			if (T::typeid == Byte::typeid)
			{
				reader->Read((array<Byte>^)values, 0, (int)lengthBytes);
				return values;
			}

			// Otherwise, read through a byte buffer
			array<Byte>^ buffer = gcnew array<Byte>(BufferLengthBytes);

			int nextByteIndex = 0;
			while (nextByteIndex < lengthBytes)
			{
				int bytesRead = reader->Read(buffer, 0, buffer->Length);
				if (bytesRead <= 0) break;

				Buffer::BlockCopy(buffer, 0, values, nextByteIndex, bytesRead);
				nextByteIndex += bytesRead;
			}

			return values;

			// TODO: Figure out how to read like this:
			//pin_ptr<T> pResult = &result[0];
			//reader->Read(pResult, 0, lengthBytes);
		}

		generic<typename T>
		void BinarySerializer::Write(BinaryWriter^ writer, array<T>^ set)
		{
			Write(writer, set, 0, set->Length);
		}

		generic<typename T>
		void BinarySerializer::Write(BinaryWriter^ writer, array<T>^ set, int index, int length)
		{
			// Default length if not provided
			if (length == -1) length = set->Length - index;

			// Return immediately if nothing to write
			if (length == 0) return;

			// Validate arguments
			if (set == nullptr) throw gcnew ArgumentNullException("array");
			if (index < 0 || index > set->Length) throw gcnew ArgumentOutOfRangeException("index");
			if (length < 0 || index + length > set->Length) throw gcnew ArgumentOutOfRangeException("length");

			// Write byte[] directly
			if (T::typeid == Byte::typeid)
			{
				writer->Write((array<Byte>^)set, index, length);
				return;
			}

			// Copy array to byte[] for serialization
			array<Byte>^ buffer = gcnew array<Byte>(BufferLengthBytes);
			int elementSize = ElementSize<T>();
			int lengthBytes = (index + length) * elementSize;

			int nextByteIndex = index * elementSize;
			while (nextByteIndex < lengthBytes)
			{
				int blockSizeBytes = lengthBytes - nextByteIndex;
				if (blockSizeBytes > buffer->Length) blockSizeBytes = buffer->Length;

				Buffer::BlockCopy(set, nextByteIndex, buffer, 0, blockSizeBytes);
				writer->Write(buffer, 0, blockSizeBytes);

				nextByteIndex += blockSizeBytes;
			}

			// TODO: Figure out how to copy like this - straight unmanaged pointer - OR...
			//  figure out how to read with native code (C++ STL I/O?)
			// Write the array as a byte[]
			//pin_ptr<T> pArray = &array[0];
			//writer->BaseStream->Write((Byte*)pArray, index * elementSize, length * elementSize);
		}
	}
}


