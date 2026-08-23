# Push Down Method

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Push Down Method
Problem
Is behavior implemented in a superclass used by only one (or a few) subclasses?
Solution
Move this behavior to the subclasses.
Before
After

 
23.08.2026, 11:12 Push Down Method
https://sourcemaking.com/refactoring/push-down-method 1/3

Why Refactor
At first a certain method was meant to be universal for all classes but in reality is used in only one
subclass. This situation can occur when planned features fail to materialize.
Such situations can also occur after partial extraction (or removal) of functionality from a class
hierarchy, leaving a method that is used in only one subclass.
If you see that a method is needed by more than one subclass, but not all of them, it may be useful
to create an intermediate subclass and move the method to it. This allows avoiding the code
duplication that would result from pushing a method down to all subclasses.
Benefits
Improves class coherence. A method is located where you expect to see it.
How to Refactor
1. Declare the method in a subclass and copy its code from the superclass.
23.08.2026, 11:12 Push Down Method
https://sourcemaking.com/refactoring/push-down-method 2/3

2. Remove the method from the superclass.
3. Find all places where the method is used and verify that it is called from the necessary subclass.
RETURN READ NEXT
Push Down Field    Pull Up Constructor Body
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
23.08.2026, 11:12 Push Down Method
https://sourcemaking.com/refactoring/push-down-method 3/3
