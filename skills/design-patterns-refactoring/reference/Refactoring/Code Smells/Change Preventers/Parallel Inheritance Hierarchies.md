# Parallel Inheritance Hierarchies

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Change Preventers
Parallel Inheritance Hierarchies
Signs and Symptoms
Whenever you create a subclass for a class, you �nd yourself needing to create a subclass for
another class.
Reasons for the Problem
All was well as long as the hierarchy stayed small. But with new classes being added, making
changes has become harder and harder.
Treatment
You may de-duplicate parallel class hierarchies in two steps. First, make instances of one
hierarchy refer to instances of another hierarchy. Then, remove the hierarchy in the referred

 

class, by using Move Method and Move Field.
Payoff
• Reduces code duplication.
• Can improve organization of code.
When to Ignore
Sometimes having parallel class hierarchies is just a way to avoid even bigger mess with
program architecture. If you �nd that your attempts to de-duplicate hierarchies produce even
uglier code, just step out, revert all of your changes and get used to that code.
READ NEXTRETURN
Dispensables  Shotgun Surgery
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more
