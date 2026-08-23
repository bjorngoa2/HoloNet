# Remove Middle Man

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Moving Features between Objects
Remove Middle Man
Problem
A class has too many methods that simply delegate to other objects.
Solution
Delete these methods and force the client to call the end methods directly.
Before
After

 
23.08.2026, 11:10 Remove Middle Man
https://sourcemaking.com/refactoring/remove-middle-man 1/3

Why Refactor
In this technique, we will use the terms from Hide Delegate, which are:
delegate — the end object that contains the functionality needed by the client
server — the object to which the client has direct access
There are two types of problems:
1. The server-class does not do anything itself and simply creates needless complexity. In this
case, give thought to whether this class is needed at all.
2. Every time a new feature is added to the delegate, you need to create a delegating method for it
in the server-class. If a lot of changes are made, this will be rather tiresome.
How to Refactor
1. Create a getter for accessing the delegate-class object from the server-class object.
2. Replace calls to delegating methods in the server-class with direct calls for methods in the
delegate-class.
23.08.2026, 11:10 Remove Middle Man
https://sourcemaking.com/refactoring/remove-middle-man 2/3

RETURN READ NEXT
Introduce Foreign Method    Hide Delegate
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
23.08.2026, 11:10 Remove Middle Man
https://sourcemaking.com/refactoring/remove-middle-man 3/3
