# Hatate
Search and tag images using IQDB.

## Dependencies
* **IqdbApi** by _ImoutoChan_:
[GitHub](https://github.com/ImoutoChan/IqdbApi)
* **dcsoup** by _matarillo_:
[GitHub](https://github.com/matarillo/dcsoup)

## What does it do?

Takes all the images (JPG, JPEG and PNG) from the selected folder and send them to IQDB.
If a match is found the file is moved to a "tagged" subfolder alongside a text file containing all the tags.
Otherwise it will be moved to a "notfound" subfolder.

## How to use

1. Start the program, click "Open folder" and select a folder containing the images you want to tag
2. Click the "Start" button
3. Leave it open until all the images were searched
	* 3.1 If you checked the "Compare" option, you will have to confirm each result

## Options

The "Options" button in the menubar allow to change some parameters:

### Keep only known tags

If checked, will compare the found tags with the ones from text fields inside a "tags" folder and only keep those that matches.
To setup this function, you will need to create a "tags" folder alongside the program executable and put some text files in it: `tags.txt`, `series.txt`, `characters.txt` and `creators.txt`.
Put one tag per line in those files. `tags.txt` is for general tags while the others are for namespaces.
Example:
```
-> Content of tags.txt:
tag1
tag2

-> Content of series.txt:
tag3
```
With that, if an image is found with the tags `tag1`, `tag2`, `tag3` and `tag4`, the text file for this image will contain:
```
tag1
tag2
series:tag3
```
Note that `tag4` is scrapped and that `tag3` has the `series` namespace before it.

### Add rating to tags:

If available the rating will be added as a tag, for example `rating:safe`.

### Minimum match type

If checked, only the results with a match type greater or equal that the selected one will be kept.
For the best results, check this option and select "Best" in the list.

### Minimum number of tags

Define how many tags are needed to keep a result. With a value of `1`, results without any tags will be scrapped.

### Minimum similarity

Define the minimum similarity value to keep a result. A lower value will reduce the accuracy but don't use a value too high as no search will produce a 100% match.

### Delay

To prevent from abusing the IQDB service and being banned from it, a certain wait time is applied between each search.
The default is 60 seconds, consider increasing it if you have a lot of time fot it to run but don't reduce it to much.

### Randomize the delay

Will use a random delay based on the one from the previous option.

### Automatically move files when no user action is needed

Not found results or found without unknown tags will be moved automatically after being searched.

### Ask for tags when adding files to the list

When importing files into the program a window will be shown asking for tags
Those tags will be added to the known tag list for each imported files.

### Parse booru page when possible

Will parse the image's booru page to retrieve namespaces tags.
If not checked the unnamespaced tags from IQDB will be used.

### Sources

Only the results from the checked sources will be kept when searching.
If an image only have results from unchecked sources it will be marked as not found.
