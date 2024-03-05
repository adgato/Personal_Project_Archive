<?php
    $command = escapeshellcmd('PATH_TO/python.exe PATH_TO/scanned_email_reader.py');
    $output = shell_exec($command);

    $url = explode('&url=', urldecode($_SERVER['HTTP_REFERER']));
    $domain = explode('?response=', $url[0])[0];

    header("Refresh:0; url=".str_replace('?', '', $domain)."?response=".urlencode($output).(count($url) > 1 ? '&url='.$url[1] : ''));
?>