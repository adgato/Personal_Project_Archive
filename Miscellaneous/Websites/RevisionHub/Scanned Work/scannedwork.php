<?php
    $command = escapeshellcmd('C:/Users/Lenovo/AppData/Local/Programs/Python/Python37/python.exe "C:/Users/Lenovo/My Files/RevisionHub/Scanned Work/scanned_email_reader.py"');
    $output = shell_exec($command);

    $url = explode('&url=', urldecode($_SERVER['HTTP_REFERER']));
    $domain = explode('?response=', $url[0])[0];

    header("Refresh:0; url=".str_replace('?', '', $domain)."?response=".urlencode($output).(count($url) > 1 ? '&url='.$url[1] : ''));
?>