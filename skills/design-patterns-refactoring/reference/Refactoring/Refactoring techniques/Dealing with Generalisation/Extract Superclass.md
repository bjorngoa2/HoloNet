# Extract Superclass

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Extract Superclass
Problem
You have two classes with common fields and methods.
Solution
Create a shared superclass for them and move all the identical fields and methods to it.
Before

 
23.08.2026, 11:12 Extract Superclass
https://sourcemaking.com/refactoring/extract-superclass 1/3

Why Refactor
One type of code duplication occurs when two classes perform similar tasks in the same way, or
perform similar tasks in different ways. Objects offer a built-in mechanism for simplifying such
situations via inheritance. But oftentimes this similarity remains unnoticed until classes are created,
necessitating that an inheritance structure be created later.
Benefits
Code deduplication. Common fields and methods now “live” in one place only.
When Not to Use
You can not apply this technique to classes that already have a superclass.
After
23.08.2026, 11:12 Extract Superclass
https://sourcemaking.com/refactoring/extract-superclass 2/3

How to Refactor
1. Create an abstract superclass.
2. Use Pull Up Field, Pull Up Method, and Pull Up Constructor Body to move the common
functionality to a superclass. Start with the fields, since in addition to the common fields you
will need to move the fields that are used in the common methods.
3. Look for places in the client code where use of subclasses can be replaced with your new class
(such as in type declarations).
RETURN READ NEXT
Extract Interface    Extract Subclass
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
23.08.2026, 11:12 Extract Superclass
https://sourcemaking.com/refactoring/extract-superclass 3/3
