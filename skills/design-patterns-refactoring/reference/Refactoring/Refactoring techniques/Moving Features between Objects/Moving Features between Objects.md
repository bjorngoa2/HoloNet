# Moving Features between Objects

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques
Moving Features betweenObjects
Even if you have distributed functionality among different classes in a less-than-perfect way, there
is still hope.
These refactoring techniques show how to safely move functionality between classes, create new
classes, and hide implementation details from public access.
Problem: A method is used more in another class than in its own class.
Solution: Create a new method in the class that uses the method the most, then move code from
the old method to there. Turn the code of the original method into a reference to the new
method in the other class or else remove it entirely.
Problem: A field is used more in another class than in its own class.
Solution: Create a field in a new class and redirect all users of the old field to it.
Problem: When one class does the work of two, awkwardness results.
Solution: Instead, create a new class and place the fields and methods responsible for the
relevant functionality in it.
§ Move Method
§ Move Field
§ Extract Class
§ Inline Class

 
23.08.2026, 11:10 Moving Features between Objects
https://sourcemaking.com/refactoring/moving-features-between-objects 1/3

Problem: A class does almost nothing and is not responsible for anything, and no additional
responsibilities are planned for it.
Solution: Solution: Move all features from the class to another one.
Problem: The client gets object B from a field or method of object А . Then the client calls a
method of object B.
Solution: Create a new method in class A that delegates the call to object B. Now the client does
not know about, or depend on, class B.
Problem: A class has too many methods that simply delegate to other objects.
Solution: Delete these methods and force the client to call the end methods directly.
Problem: A utility class does not contain the method that you need and you cannot add the
method to the class.
Solution: Add the method to a client class and pass an object of the utility class to it as an
argument.
Problem: A utility class does not contain some methods that you need. But you cannot add these
methods to the class.
Solution: Create a new class containing the methods and make it either the child or wrapper of
the utility class.
§ Hide Delegate
§ Remove Middle Man
§ Introduce Foreign Method
§ Introduce Local Extension
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:10 Moving Features between Objects
https://sourcemaking.com/refactoring/moving-features-between-objects 2/3

RETURN READ NEXT
Move Method    Substitute Algorithm
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:10 Moving Features between Objects
https://sourcemaking.com/refactoring/moving-features-between-objects 3/3
