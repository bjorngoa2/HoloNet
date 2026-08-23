# Collapse Hierarchy

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Collapse Hierarchy
Problem
You have a class hierarchy in which a subclass is practically the same as its superclass.
Solution
Merge the subclass and superclass.
Before
After

 
23.08.2026, 11:12 Collapse Hierarchy
https://sourcemaking.com/refactoring/collapse-hierarchy 1/3

Why Refactor
Your program has grown over time and a subclass and superclass have become practically the
same. A feature was removed from a subclass, a method was moved to the superclass... and now
you have two look-alike classes.
Benefits
Program complexity is reduced. Fewer classes mean fewer things to keep straight in your head
and fewer breakable moving parts to worry about during future code changes.
Navigating through your code is easier when methods are defined in one class early. You do not
need to comb through the entire hierarchy to find a particular method.
When Not to Use
Does the class hierarchy that you are refactoring have more than one subclass? If so, after
refactoring is complete, the remaining subclasses should become the inheritors of the class in
which the hierarchy was collapsed.
But keep in mind that this can lead to violations of the Liskov substitution principle. For
example, if your program emulates city transport networks and you accidentally collapse the
Transport superclass into the Car subclass, then the Plane class may become the inheritor of
Car. Oops!
How to Refactor
1. Select which class is easier to remove: the superclass or its subclass.
23.08.2026, 11:12 Collapse Hierarchy
https://sourcemaking.com/refactoring/collapse-hierarchy 2/3

2. Use Pull Up Field and Pull Up Method if you decide to get rid of the subclass. If you choose to
eliminate the superclass, go for Push Down Field and Push Down Method.
3. Replace all uses of the class that you are deleting with the class to which the fields and
methods are to be migrated. Often this will be code for creating classes, variable and parameter
typing, and documentation in code comments.
4. Delete the empty class.
RETURN READ NEXT
Form Template Method    Extract Interface
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
23.08.2026, 11:12 Collapse Hierarchy
https://sourcemaking.com/refactoring/collapse-hierarchy 3/3
