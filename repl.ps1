param(
  [Parameter()]
  [switch]
  $Release

)
$name = Get-Item $PSScriptRoot | ForEach-Object Name
$dir = if ($Release.IsPresent)
{
  Join-Path $PSScriptRoot CueSheetNet bin debug
}
else
{
  Join-Path $PSScriptRoot CueSheetNet bin release
  }

$dll=  $dir = Get-ChildItem -Recurse -File -Filter "$name.dll" | ForEach-Object FullName

. csharprepl  -r $dll -u CueSheetNet
