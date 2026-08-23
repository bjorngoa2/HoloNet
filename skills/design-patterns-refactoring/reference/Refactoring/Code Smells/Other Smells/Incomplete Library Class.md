# Incomplete Library Class

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Other Smells
Incomplete Library Class
Signs and Symptoms
Sooner or later, libraries stop meeting user needs. The only solution to the problem – changing
the library – is often impossible since the library is read-only.
Reasons for the Problem
The author of the library has not provided the features you need or has refused to implement
them.
Treatment
• T o introduce a few methods to a library class, use Introduce Foreign Method.
• For big changes in a class library, use Introduce Local Extension.
Payoff
Reduces code duplication (instead of creating your own library from scratch, you can still piggy-
back off an existing one).
When to Ignore
Extending a library can generate additional work if the changes to the library involve changes
in code.

 

READ NEXTRETURN
Refactoring techniques  Other Smells
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
