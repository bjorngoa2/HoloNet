# Add Parameter

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Add Parameter
Problem
A method does not have enough data to perform certain actions.
Solution
Create a new parameter to pass the necessary data.
Before
After

 
23.08.2026, 11:11 Add Parameter
https://sourcemaking.com/refactoring/add-parameter 1/3

Why Refactor
You need to make changes to a method and these changes require adding information or data that
was previously not available to the method.
Benefits
The choice here is between adding a new parameter and adding a new private field that contains
the data needed by the method. A field is preferable when you need some occasional or frequently
changing data for which there is no point in holding it in an object all of the time. In this case, a
new parameter will be a better fit than a private field and the refactoring will pay off. Otherwise,
add a private field and fill it with the necessary data before calling the method.
Drawbacks
Adding a new parameter is always easier than removing it, which is why parameter lists
frequently balloon to grotesque sizes. This smell is known as the Long Parameter List.
If you need to add a new parameter, sometimes this means that your class does not contain the
necessary data or the existing parameters do not contain the necessary related data. In both
cases, the best solution is to consider moving data to the main class or to other classes whose
objects are already accessible from inside the method.
How to Refactor
1. See whether the method is defined in a superclass or subclass. If the method is present in them,
you will need to repeat all the steps in these classes as well.
2. The following step is critical for keeping your program functional during the refactoring process.
Create a new method by copying the old one and add the necessary parameter to it. Replace the
code for the old method with a call to the new method. You can plug in any value to the new
parameter (such as null for objects or a zero for numbers).
3. Find all references to the old method and replace them with references to the new method.
4. Delete the old method. Deletion is not possible if the old method is part of the public interface.
If that is the case, mark the old method as deprecated.
23.08.2026, 11:11 Add Parameter
https://sourcemaking.com/refactoring/add-parameter 2/3

RETURN READ NEXT
Remove Parameter    Rename Method
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
23.08.2026, 11:11 Add Parameter
https://sourcemaking.com/refactoring/add-parameter 3/3
