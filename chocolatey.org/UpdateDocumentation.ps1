if ($null -eq (Get-ChildItem -Path 'choco.wiki' -File)) {
    git submodule init
}

git submodule update --remote --rebase

# copy the images
Copy-Item "choco.wiki\images\*" "chocolatey\Website\content\images\docs" -Force -Recurse

# copy the md
Copy-Item "choco.wiki\**\*.md" "chocolatey\Website\Views\Documentation\Files" -Force -Recurse
