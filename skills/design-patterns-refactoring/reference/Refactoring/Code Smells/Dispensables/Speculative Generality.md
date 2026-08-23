# Speculative Generality

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Dispensables
Speculative Generality
Signs and Symptoms
There is an unused class, method, �eld or parameter.
Reasons for the Problem
Sometimes code is created “just in case” to support anticipated future features that never get
implemented. As a result, code becomes hard to understand and support.
Treatment
For removing unused abstract classes, try Collapse Hierarchy.

 

• Unnecessary delegation of functionality to another class can be eliminated via Inline Class.
• Unused methods? Use Inline Method to get rid of them.
• Methods with unused parameters should be given a look with the help of Remove
Parameter.
• Unused �elds can be simply deleted.
Payoff
• Slimmer code.
• Easier support.
When to Ignore
• If you are working on a framework, it is eminently reasonable to create functionality not
used in the framework itself, as long as the functionality is needed by the frameworks’s
users.
• Before deleting elements, make sure that they are not used in unit tests. This happens if
tests need a way to get certain internal information from a class or perform special testing-
related actions.
Reading is boring
Aren't you bored of reading so much? Try out our new interactive

READ NEXTRETURN
Couplers  Dead CodeDesign Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
learning course on refactoring. It has more content and much more
fun.
 Learn more
