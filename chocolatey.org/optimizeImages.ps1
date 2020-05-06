$scriptDir = $(Split-Path -parent $MyInvocation.MyCommand.Definition)
$webTopDirectory = '.\chocolatey\Website'

cinst pngoptimizer.commandline
#cinst jpegoptimizer

# get all files that meet *.png pattern
Get-ChildItem "$webTopDirectory" -Filter *.png -Recurse | %{
  #write-host "found $($_.FullName)"
  & pngoptimizercl -file:"$($_.FullName)" -IgnoreAnimatedGifs 
}

# get all files that meet *.jpg pattern
Get-ChildItem "$webTopDirectory" -Filter *.jpg -Recurse | %{
  #write-host "found $($_.FullName)"
  #& jpegoptimizer "$($_.FullName)"
}