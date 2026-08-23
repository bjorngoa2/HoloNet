# Couplers

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells
Couplers
All the smells in this group contribute to excessive coupling between classes or show what
happens if coupling is replaced by excessive delegation.
A method accesses the data of another object more than its own data.
One class uses the internal �elds and methods of another class.
In code you see a series of calls resembling
$a->b()->c()->d()
If a class performs only one action, delegating work to another class, why does it exist at all?
READ NEXTRETURN
§Feature Envy
§Inappropriate Intimacy
§Message Chains
§Middle Man
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more

 

Feature Envy  Speculative Generality
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
