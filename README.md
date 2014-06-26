CR_RemoveStringToken
====================

This plugin adds a new CodeProvider '**Remove String Token**' 

This CodeProvider allows the user to remove a String Token from a string, along with it's associated argument.

###Example Usage

Starting with...

	var MyString = String.Format("Hello {0}", "World");

 * Place the caret within **{0}**
 * Choose '**Remove String Token**' from the CodeRush Smart Tag menu.

CodeRush will alter the above example to...

	var MyString = String.Format("Hello ");
