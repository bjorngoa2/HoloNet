# Extract Interface

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Extract Interface
Problem
Multiple clients are using the same part of a class interface. Another case: part of the interface in
two classes is the same.
Solution
Move this identical portion to its own interface.
Before
After

 
23.08.2026, 11:12 Extract Interface
https://sourcemaking.com/refactoring/extract-interface 1/3

Why Refactor
1. Interfaces are very apropos when classes play special roles in different situations. Use Extract
Interface to explicitly indicate which role.
2. Another convenient case arises when you need to describe the operations that a class performs
on its server. If it is planned to eventually allow use of servers of multiple types, all servers must
implement the interface.
Good to Know
There is a certain resemblance between Extract Superclass and Extract Interface.
Extracting an interface allows isolating only common interfaces, not common code. In other words,
if classes contain Duplicate Code, extracting the interface will not help you to deduplicate.
23.08.2026, 11:12 Extract Interface
https://sourcemaking.com/refactoring/extract-interface 2/3

All the same, this problem can be mitigated by applying Extract Class to move the behavior that
contains the duplication to a separate component and delegating all the work to it. If the common
behavior is large in size, you can always use Extract Superclass. This is even easier, of course, but
remember that if you take this path you will get only one parent class.
How to Refactor
1. Create an empty interface.
2. Declare common operations in the interface.
3. Declare the necessary classes as implementing the interface.
4. Change type declarations in the client code to use the new interface.
RETURN READ NEXT
Collapse Hierarchy    Extract Superclass
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
23.08.2026, 11:12 Extract Interface
https://sourcemaking.com/refactoring/extract-interface 3/3
