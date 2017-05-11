PUSHD "%~dp0..\.."
bin\Release\Arriba.Csv.exe /mode:build /table:Rates /csvPath:"%~dp0PowerRatesByZip.csv" /maximumCount:60000
POPD