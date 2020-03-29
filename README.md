# Hatate
Search and tag images using IQDB.

Tagged images can then be imported into [Hydrus](https://github.com/hydrusnetwork/hydrus).

## Dependencies
* **IqdbApi** by _ImoutoChan_:
[GitHub](https://github.com/ImoutoChan/IqdbApi)
* **dcsoup** by _matarillo_:
[GitHub](https://github.com/matarillo/dcsoup)

## How to use

1. Download the latest release [here](https://github.com/nostrenz/hatate-iqdb-tagger/releases)
2. Start the program and [add images](#Adding-images)
3. Click the "**Start**" button
4. Leave it open and wait for the images to be searched and tagged.

![Main window](https://raw.githubusercontent.com/nostrenz/hatate-iqdb-tagger/master/screenshots/window.png)

## 1) Adding images

You can add images in various ways:

### By drag & drop

Nothing special, just drag some images from a folder or directly from Hydrus' image grid.

### By using the "Add files" or "Add folder" menus

You can manually add files or a whole folder using those entries from the "**Files**" menu.

### By executing a Hydrus query

[**This requires Hydrus to be running and the API to be configured.**](#Settings->-Hydrus-API)

The "**Files > Query Hydrus**" menu opens a window allowing you to enter tags and matching files from Hydrus will be added.

## 2) Reviewing results

After being searched, each file in the list will be colored depending on the result:

- **Green** means the image was found on IQDB and tags were retrieved.
- **Yellow** means the image was found on IQDB but very few tags were retrieved or the booru image seems of better quality than the local one. Either way, you should review it.
- **Red** means the image was not found on IQDB.

## 3) Sending tags to Hydrus

Once a file is searched you'll probably want to send it and its tags to Hydrus.
You can do that by selecting one or more files in the list then right-clicking and selecting one of those options:

### Write tags to text files

This will write a text file next to the image with all the tags in it. You can then import that image into Hydrus by clicking on the "**add tags based on filename**" button at the bottom of the import window then checking "**try to load tags from neighbouring .txt files**".

### Send tags to Hydrus

[**This requires Hydrus to be running and the API to be configured.**](#Settings->-Hydrus-API)

Files will be directly imported into Hydrus with tags through the API.

If an error happens during the process the row will be colored in **orange** and placing the mouse cursor over the row will show you a tooltip about the problem.

### Send URLs to Hydrus

[**This requires Hydrus to be running and the API to be configured.**](#Settings->-Hydrus-API)

The local file won't be sent to Hydrus, instead the matched booru URL will be sent making Hydrus download it.
By default tags will also be sent alongside the URL but you can [disable that if you want](#Send-tags-alonside-a-URL).

If the file is colored in **Yellow** you should probably use this option as the booru image seems better than the local one.

If an error happens during the process the row will be colored in **orange** and placing the mouse cursor over the row will show you a tooltip about the problem.

---

## Settings > Options

The "**Options**" button in the menubar allows to change some parameters:

### Add rating to tags:

If available the rating will be added as a tag, for example `rating:safe`.

### Minimum match type

If checked, only results with a match type greater or equal than the selected one will be kept.
For the best results, check this option and select "**Best**" in the list.

### Minimum number of tags

Define how many tags are needed to keep a result. With a value of `1`, results without any tags will be marked as not found.

### Minimum similarity

Define the minimum similarity value to keep a result. A lower value will reduce the accuracy but don't use a value too high as no search will produce a 100% match.

### Delay

To prevent from abusing the IQDB service and being banned from it, a certain wait time is applied between each search.
Default is 60 seconds, consider increasing it if you have a lot of time for it to run but don't reduce it to much (30 seconds should be ok).

### Randomize the delay

Will use a random delay based on the Delay option.

### Ask for tags when adding files to the list

When importing files into the program a window will be shown asking for tags.
Those tags will be added to the tags list for each imported files.

### Log matched URLs into a text file

If checked, booru URLs will be logged into a text file if an image is found.
You can open this file from the "**Files > Open**" matched URLs menu.

### Retrieve booru tags

By default tags will be retrieved from the matched booru page after a successful IQDB search.
You can disable this here and just use the IQDB search function without retrieving tags.
Useful if you only want to use the "[**Send URLs to Hydrus**](#Send-URLs-to-Hydrus)" option and let Hydrus get tags using its own parsers.

### Add this tag to files found on IQDB

If a file is successfully found on IQDB (so **green** or **yellow** row in the list) then this tag will be added to it.

### Add this tag to files not found on IQDB

If a file wasn't found on IQDB (so **red** row in the list) then this tag will be added to it.

### Add this tag to tagged files

If at least some tags were retrieved by parsing the matched booru page, then this tag will be added to the file.

### Sources

Only results from the checked sources will be kept when searching.
If an image only have results from unchecked sources it will be marked as not found.

---

## Settings > Hydrus API

This menu is dedicated to set up the connection to the Hydrus API, as well as some other options relative to it.

First you need to set the API host, post and access key.
[Please refer to the Hydrus documentation to know how to set up the API in Hydrus](http://hydrusnetwork.github.io/hydrus/help/client_api.html).
Once this is done, click the "**Test connection**" button. If successful, select one of the tag service and click "**Save**".

Here's the other available options:

### Automatically send searched files to Hydrus

If this is checked, files will be sent to Hydrus right after being searched. So no need to right-click them and select a way to send the tags.

### Send files to recycle bin once imported into Hydrus

If this is checked, files will be sent to the recycle bin after selecting "[**Send tags to Hydrus**](#Send-tags-to-Hydrus)" or "[**Send URLs to Hydrus**](#Send-URLs-to-Hydrus)".

### Associate found URL with file when sending tags

If this is checked, the matched URL will be added to the "known urls" in Hydrus for each imported file.

### Send tags alongside a URL

If this is checked, tags will also be sent when sending a URL to Hydrus.
