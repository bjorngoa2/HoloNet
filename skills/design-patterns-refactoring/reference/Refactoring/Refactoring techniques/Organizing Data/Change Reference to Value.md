# Change Reference to Value

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Organizing Data
Change Reference to Value
Problem
You have a reference object that is too small and infrequently changed to justify managing its life
cycle.
Solution
Turn it into a value object.
Before
After

 
23.08.2026, 11:11 Change Reference to Value
https://sourcemaking.com/refactoring/change-reference-to-value 1/3

Why Refactor
Inspiration to switch from a reference to a value may come from the inconvenience of working with
the reference. References require management on your part:
They always require requesting the necessary object from storage.
References in memory may be inconvenient to work with.
Working with references is particularly difficult, compared to values, on distributed and parallel
systems.
Values are especially useful if you would rather have unchangeable objects than objects whose
state may change during their lifetime.
Benefits
One important property of objects is that they should be unchangeable. The same result should
be received for each query that returns an object value. If this is true, no problems arise if there
are many objects representing the same thing.
Values are much easier to implement.
Drawbacks
If a value is changeable, make sure if any object changes that the values in all the other objects
representing the same entity are updated. This is so burdensome that it is easier to create a
reference for this purpose.
23.08.2026, 11:11 Change Reference to Value
https://sourcemaking.com/refactoring/change-reference-to-value 2/3

How to Refactor
1. Make the object unchangeable. The object should not have any setters or other methods that
change its state and data (Remove Setting Method may help here). The only place where data
should be assigned to the fields of a value object is a constructor.
2. Create a comparison method to be able to compare two values.
3. Check whether you can delete the factory method and make the object constructor public.
RETURN READ NEXT
Replace Array with Object    Change Value to Reference
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
23.08.2026, 11:11 Change Reference to Value
https://sourcemaking.com/refactoring/change-reference-to-value 3/3
