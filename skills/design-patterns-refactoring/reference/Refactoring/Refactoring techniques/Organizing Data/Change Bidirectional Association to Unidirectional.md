# Change Bidirectional Association to Unidirectional

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Organizing Data
Change Bidirectional Associationto Unidirectional
Problem
You have a bidirectional association between classes, but one of the classes does not use the
other’s features.
Solution
Remove the unused association.
Before
After

 
23.08.2026, 11:11 Change Bidirectional Association to Unidirectional
https://sourcemaking.com/refactoring/change-bidirectional-association-to-unidirectional 1/3

Why Refactor
A bidirectional association is generally harder to maintain than a unidirectional one, requiring
additional code for properly creating and deleting the relevant objects. This makes the program
more complicated.
In addition, an improperly implemented bidirectional association can cause problems for garbage
collection (in turn leading to memory bloat by unused objects).
Example: the garbage collector removes objects from memory that are no longer referenced by
other objects. Let’s say that an object pair User-Order was created, used, and then abandoned. But
these objects will not be cleared from memory since they still refer to each other. That said, this
problem is becoming less important thanks to advances in programming languages, which now
automatically identify unused object references and remove them from memory.
There is also the problem of interdependency between classes. In a bidirectional association, the
two classes must know about each other, meaning that they cannot be used separately. If many of
these associations are present, different parts of the program become too dependent on each other
and any changes in one component may affect the other components.
Benefits
Simplifies the class that does not need the relationship. Less code equals less code
maintenance.
Reduces dependency between classes. Independent classes are easier to maintain since any
changes to a class affect only that class.
How to Refactor
Make sure that one of the following is true for your classes:
No association is used.
There is another way to get the associated object, such through a database query.
The associated object can be passed as an argument to the methods that use it.
23.08.2026, 11:11 Change Bidirectional Association to Unidirectional
https://sourcemaking.com/refactoring/change-bidirectional-association-to-unidirectional 2/3

2. Depending on your situation, use of a field that contains an association with another object
should be replaced by a parameter or method call for getting the object in a different way.
3. Delete the code that assigns the associated object to the field.
4. Delete the now-unused field.
RETURN READ NEXT
Replace Magic Number with Symbolic
Constant    Change Unidirectional Association to
Bidirectional
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:11 Change Bidirectional Association to Unidirectional
https://sourcemaking.com/refactoring/change-bidirectional-association-to-unidirectional 3/3
