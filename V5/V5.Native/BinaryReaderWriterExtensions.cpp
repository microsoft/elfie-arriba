#include "stdafx.h"
#include "BinaryReaderWriterExtensions.h"

namespace V5
{
	namespace Serialization
	{
		//generic <typename T>
		//int BinaryReaderWriterExtensions::ElementSize()
		//{
		//	if (T::typeid == Boolean::typeid) return 1;
		//	return System::Runtime::InteropServices::Marshal::SizeOf<T>();
		//}

		//generic <typename T>
		//array<T>^ BinaryReaderWriterExtensions::ReadArray(BinaryReader^ reader, __int64 lengthBytes)
		//{
		//	int elementSize = ElementSize<T>();
		//	array<T>^ result = gcnew array<T>((int)(lengthBytes / elementSize));

		//	//pin_ptr<T> pResult = &result[0];
		//	//reader->Read(pResult, 0, lengthBytes);

		//	return result;
		//}

		//generic<typename T>
		//void BinaryReaderWriterExtensions::Write(BinaryWriter^ writer, array<T>^ a, int index, int length)
		//{
		//	// Default length if not provided
		//	if (length == -1) length = a->Length - index;

		//	// Return immediately if nothing to write
		//	if (length == 0) return;

		//	// Validate arguments
		//	if (a == nullptr) throw gcnew ArgumentNullException("array");
		//	if (index < 0 || index > a->Length) throw gcnew ArgumentOutOfRangeException("index");
		//	if (length < 0 || index + length > a->Length) throw gcnew ArgumentOutOfRangeException("length");

		//	// TODO: Copy the 'Copy to buffer in blocks' approach, at least
		//	array<Byte>^ buffer = gcnew array<Byte>[32768];

		//	if (T::typeid == Byte::typeid)
		//	{
		//		writer->Write(dynamic_cast<array<Int32>^>(a), index, length);
		//		return;
		//	}

		//	// Find the element size
		//	int elementSize = ElementSize<T>();

		//	// TODO: Figure out how to copy like this - straight unmanaged pointer - OR...
		//	//  figure out how to read with native code (C++ STL I/O?)
		//	// Write the array as a byte[]
		//	//pin_ptr<T> pArray = &array[0];
		//	//writer->BaseStream->Write((Byte*)pArray, index * elementSize, length * elementSize);
		//}
	}
}


