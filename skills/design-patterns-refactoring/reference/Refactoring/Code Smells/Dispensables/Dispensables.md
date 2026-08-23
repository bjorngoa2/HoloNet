# Dispensables

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells
Dispensables
A dispensable is something pointless and unneeded whose absence would make the code
cleaner, more ef�cient and easier to understand.
A method is �lled with explanatory comments.
T wo code fragments look almost identical.
Understanding and maintaining classes always costs time and money. So if a class doesn’t do
enough to earn your attention, it should be deleted.
A data class refers to a class that contains only �elds and crude methods for accessing them
(getters and setters). These are simply containers for data used by other classes. These
classes do not contain any additional functionality and cannot independently operate on the
data that they own.
A variable, parameter, �eld, method or class is no longer used (usually because it is obsolete).
There is an unused class, method, �eld or parameter.
§Comments
§Duplicate Code
§Lazy Class
§Data Class
§Dead Code
§Speculative Generality
Reading is boring

 

READ NEXTRETURN
Comments  Parallel Inheritance Hierarchies
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more
