# Inappropriate Intimacy

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Couplers
Inappropriate Intimacy
Signs and Symptoms
One class uses the internal �elds and methods of another class.
Reasons for the Problem
Keep a close eye on classes that spend too much time together. Good classes should know as
little about each other as possible. Such classes are easier to maintain and reuse.
Treatment
The simplest solution is to use Move Method and Move Field to move parts of one class to the
class in which those parts are used. But this works only if the �rst class truly does not need
these parts.

 

• Another solution is to use Extract Class and Hide Delegate on the class to make the code
relations “of�cial”.
• If the classes are mutually interdependent, you should use Change Bidirectional Association
to Unidirectional.
• If this “intimacy” is between a subclass and the superclass, consider Replace Delegation with
Inheritance.
Payoff
• Improves code organization.
• Simpli�es support and code reuse.
Reading is boring

READ NEXTRETURN
Message Chains  Feature Envy
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
Aren't you bored of reading so much? Try out our new interactive learning course on
refactoring. It has more content and much more fun.
 Learn more
