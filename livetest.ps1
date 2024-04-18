$exist = where.exe csharprepl.exe
if(-not $exist){
    Write-Host "Insta"
    Write-Host "C# REPL is not installed. Install it with 'dotnet tool install -g csharprepl'" -ea stop
}
$dllPath = join-path $PSScriptRoot "CueSheetNet.Prototyper\bin\Debug\net8.0\CueSheetNet.dll"

$params=@{
    r = $dllPath
    u = 'CueSheetNet'
}
pushd $PSScriptRoot
csharprepl  $PSScriptRoot/setup.csx -r $dllPath -u CueSheetNet
popd