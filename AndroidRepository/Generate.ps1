Remove-Item ./generated/ -Filter *.cs
& xscgen --nf="namespace-mappings.txt" --output="./generated/" ./xsd/*.xsd --verbose