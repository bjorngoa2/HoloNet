# Divergent Change

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Change Preventers
Divergent Change
Divergent Change resembles Shotgun Surgery but is actually the opposite smell.
Divergent Change is when many changes are made to a single class. Shotgun Surgery
refers to when a single change is made to multiple classes simultaneously.
Signs and Symptoms
You �nd yourself having to change many unrelated methods when you make changes to a class.
For example, when adding a new product type you have to change the methods for �nding,
displaying, and ordering products.
Reasons for the Problem

 

Often these divergent modi�cations are due to poor program structure or "copypasta
programming”.
Treatment
• Split up the behavior of the class via Extract Class.
• If different classes have the same behavior, you may want to combine the classes through
inheritance (Extract Superclass and Extract Subclass).
Payoff
• Improves code organization.
• Reduces code duplication.
• Simpli�es support.
READ NEXTRETURN
Shotgun Surgery  Change Preventers
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more

© 2007-2026 SourceMaking.com T erms / Privacy policy
All rights reserved.
