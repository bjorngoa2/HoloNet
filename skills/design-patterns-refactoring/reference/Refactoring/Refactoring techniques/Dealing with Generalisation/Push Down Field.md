# Push Down Field

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Push Down Field
Problem
Is a field used only in a few subclasses?
Solution
Move the field to these subclasses.
Before
After

 
23.08.2026, 11:12 Push Down Field
https://sourcemaking.com/refactoring/push-down-field 1/3

Why Refactor
Although it was planned to use a field universally for all classes, in reality the field is used only in
some subclasses. This situation can occur when planned features fail to pan out, for example.
This can also occur due to extraction (or removal) of part of the functionality of class hierarchies.
Benefits
Improves internal class coherency. A field is located where it is actually used.
When moving to several subclasses simultaneously, you can develop the fields independently of
each other. This does create code duplication, yes, so push down fields only when you really do
intend to use the fields in different ways.
How to Refactor
1. Declare a field in all the necessary subclasses.
23.08.2026, 11:12 Push Down Field
https://sourcemaking.com/refactoring/push-down-field 2/3

2. Remove the field from the superclass.
RETURN READ NEXT
Extract Subclass    Push Down Method
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
23.08.2026, 11:12 Push Down Field
https://sourcemaking.com/refactoring/push-down-field 3/3
