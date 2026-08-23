# Middle Man

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Couplers
Middle Man
Signs and Symptoms
If a class performs only one action, delegating work to another class, why does it exist at all?
Reasons for the Problem
This smell can be the result of overzealous elimination of Message Chains.
In other cases, it can be the result of the useful work of a class being gradually moved to other
classes. The class remains as an empty shell that does not do anything other than delegate.
Treatment
If most of a method’s classes delegate to another class, Remove Middle Man is in order.

 

Payoff
Less bulky code.
When to Ignore
Do not delete middle man that have been created for a reason:
• A middle man may have been added to avoid interclass dependencies.
• Some design patterns create middle man on purpose (such as Proxy and Decorator).
READ NEXTRETURN
Other Smells  Message Chains
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
