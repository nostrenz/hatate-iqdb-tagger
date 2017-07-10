# Hatate
Search and tag images using IQDB.

## Dependencies
* **IqdbApi** by _ImoutoChan_:
[GitHub](https://github.com/ImoutoChan/IqdbApi)

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

The "Options" button in the menubar allow to chnge some parameters:

* Compare with found result before writing tags

If checked, a window presenting the original image and the result from IQDB will be shown after each search.
If the result is correct, you will need to click the "Good?" button before the process can continue.
Otherwise just close the window.

* Keep only known tags

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

* Match type

If checked, only the results with the same match type as selected in the list will be keept.
For the best results, check this option and select "Best" in the list.

* Minimum number of tags

Define how many tags are needed to keep a result. With a value of `1`, results without any tags will be scrapped.

* Minimum similarity

Define the minimum similarity value to keep a result. A lower value will reduce the accuracy but don't use a value too high as no search will produce a 100% match.

* Delay

To prevent from abusing the IQDB service and being banned from it, a certain wait time is applied between each search.
The default is 60 seconds, consider increasing it if you have a lot of time fot it to run but don't reduce it to much.
