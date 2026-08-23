# Inline Class

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Moving Features between Objects
Inline Class
Problem
A class does almost nothing and is not responsible for anything, and no additional responsibilities
are planned for it.
Solution
Solution: Move all features from the class to another one.
Before
After

 
23.08.2026, 11:10 Inline Class
https://sourcemaking.com/refactoring/inline-class 1/3

Why Refactor
Often this technique is needed after the features of one class are “transplanted” to other classes,
leaving that class with little to do.
Benefits
Eliminating needless classes frees up operating memory on the computer – and bandwidth in your
head.
How to Refactor
1. In the recipient class, create the public fields and methods present in the donor class. Methods
should refer to the equivalent methods of the donor class.
2. Replace all references to the donor class with references to the fields and methods of the
recipient class.
3. Now test the program and make sure that no errors have been added. If tests show that
everything is working A-OK, start using Move Method and Move Field to completely transplant
all functionality to the recipient class from the original one. Continue doing so until the original
class is completely empty.
4. Delete the original class.
23.08.2026, 11:10 Inline Class
https://sourcemaking.com/refactoring/inline-class 2/3

RETURN READ NEXT
Hide Delegate    Extract Class
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
23.08.2026, 11:10 Inline Class
https://sourcemaking.com/refactoring/inline-class 3/3
