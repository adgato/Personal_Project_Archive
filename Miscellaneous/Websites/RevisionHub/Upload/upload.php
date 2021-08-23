<?php
$target_file = (str_contains($_SERVER['HTTP_REFERER'], '&url=') ? str_replace("/", "\\", explode('&url=', urldecode($_SERVER['HTTP_REFERER']))[1]).'\\' : '').$_POST["fileName"];
$uploadOk = 1;

// Check if file already exists
if (file_exists($target_file)) {
    echo "Sorry, file already exists.";
    $uploadOk = 0;
}

// Check if file size > 100MB
if ($_FILES["fileToUpload"]["size"] > 10000000) {
    echo "Sorry, your file is too large.";
    $uploadOk = 0;
}

// Check if $uploadOk is set to 0 by an error
if ($uploadOk == 0) {
    echo "Your file was not uploaded.";
// if everything is ok, try to upload file
} else {
    if (move_uploaded_file($_FILES["fileToUpload"]["tmp_name"], $target_file)) {
        header("Refresh:0; url=".$_SERVER['HTTP_REFERER']);
        exit;
    } else {
        echo "Sorry, there was an error uploading your file.";
    }
}
?>