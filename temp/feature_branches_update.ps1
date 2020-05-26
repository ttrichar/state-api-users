git fetch --all
git checkout integration
git status 
git pull

$branches = git branch -r -l
Write-Host $branches

foreach ($b in $branches)
{
  if($b.Contains("feature"))
  {
    $origin = $b.Trim()
    $featurename = $origin.Replace('origin/', '')
    git checkout $featurename
    git status
    git pull
    git merge integration
    $status = git status
    if($status.Contains("conflict"))
    {
        git merge --abort
        #send out notification
    }
    else
    {
        git add .
        git commit -m "updated $featurename with integration"
        git push origin
        Write-Host "merged integration into $featurename"    
    } 
  }
    
}