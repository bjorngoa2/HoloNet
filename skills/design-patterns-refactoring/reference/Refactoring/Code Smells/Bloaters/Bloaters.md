# Bloaters

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells
Bloaters
Bloaters are code, methods and classes that have increased to such gargantuan proportions
that they are hard to work with. Usually these smells do not crop up right away, rather they
accumulate over time as the program evolves (and especially when nobody makes an effort to
eradicate them).
A method contains too many lines of code. Generally, any method longer than ten lines
should make you start asking questions.
A class contains many �elds/methods/lines of code.
▪ Use of primitives instead of small objects for simple tasks (such as currency, ranges, special
strings for phone numbers, etc.)
▪ Use of constants for coding information (such as a constant USER_ADMIN_ROLE = 1  for
referring to users with administrator rights.)
▪ Use of string constants as �eld names for use in data arrays.
More than three or four parameters for a method.
Sometimes different parts of the code contain identical groups of variables (such as
parameters for connecting to a database). These clumps should be turned into their own
classes.
§Long Method
§Large Class
§Primitive Obsession
§Long Parameter List
§Data Clumps

 

READ NEXTRETURN
Long Method  Code Smells
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more
