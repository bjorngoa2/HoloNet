# Object-Orientation Abusers

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells
Object-Orientation Abusers
All these smells are incomplete or incorrect application of object-oriented programming
principles.
You have a complex
switch  operator or sequence of
if  statements.
T emporary �elds get their values (and thus are needed by objects) only under certain
circumstances. Outside of these circumstances, they are empty.
If a subclass uses only some of the methods and properties inherited from its parents, the
hierarchy is off-kilter. The unneeded methods may simply go unused or be rede�ned and give
off exceptions.
T wo classes perform identical functions but have different method names.
§Switch Statements
§T emporary Field
§Refused Bequest
§Alternative Classes with Different Interfaces
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more

 

READ NEXTRETURN
Switch Statements  Data Clumps
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
