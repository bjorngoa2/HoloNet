# Lazy Class

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Dispensables
Lazy Class
Signs and Symptoms
Understanding and maintaining classes always costs time and money. So if a class doesn’t do
enough to earn your attention, it should be deleted.
Reasons for the Problem
Perhaps a class was designed to be fully functional but after some of the refactoring it has
become ridiculously small.
Or perhaps it was designed to support future development work that never got done.
Treatment
Components that are near-useless should be given the Inline Class treatment.

 

For subclasses with few functions, try Collapse Hierarchy.
Payoff
• Reduced code size.
• Easier maintenance.
When to Ignore
Sometimes a Lazy Class is created in order to delineate intentions for future development, In
this case, try to maintain a balance between clarity and simplicity in your code.
READ NEXTRETURN
Data Class  Duplicate Code
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
