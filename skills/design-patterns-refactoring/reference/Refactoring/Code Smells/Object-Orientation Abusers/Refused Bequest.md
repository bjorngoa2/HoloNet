# Refused Bequest

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Object-Orientation Abusers
Refused Bequest
Signs and Symptoms
If a subclass uses only some of the methods and properties inherited from its parents, the
hierarchy is off-kilter. The unneeded methods may simply go unused or be rede�ned and give
off exceptions.
Reasons for the Problem
Someone was motivated to create inheritance between classes only by the desire to reuse the
code in a superclass. But the superclass and subclass are completely different.
Treatment
If inheritance makes no sense and the subclass really does have nothing in common with the
superclass, eliminate inheritance in favor of Replace Inheritance with Delegation.

 

If inheritance is appropriate, get rid of unneeded �elds and methods in the subclass. Extract all
�elds and methods needed by the subclass from the parent class, put them in a new subclass,
and set both classes to inherit from it (Extract Superclass).
Payoff
Improves code clarity and organization. You will no longer have to wonder why the Dog  class is
inherited from the Chair  class (even though they both have 4 legs).
Reading is boring
Aren't you bored of reading so much? Try out our new interactive

READ NEXT
RETURN
Alternative Classes with Different
Interfaces   T emporary FieldDesign Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
learning course on refactoring. It has more content and much more
fun.
 Learn more
