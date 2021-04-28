#!/bin/bash

if [ -f MODIFICATIONS.md ] ; then rm MODIFICATIONS.md; fi

html-diff 2fbfd2d15bdb15164d29a8557f274cdf607ee798 > MODIFICATIONS.html
git add MODIFICATIONS.html