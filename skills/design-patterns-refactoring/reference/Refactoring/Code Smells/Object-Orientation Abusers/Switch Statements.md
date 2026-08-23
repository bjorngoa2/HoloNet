# Switch Statements

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Object-Orientation Abusers
Switch Statements
Signs and Symptoms
You have a complex
switch  operator or sequence of
if  statements.
Reasons for the Problem
Relatively rare use of
switch  and
case  operators is one of the hallmarks of object-oriented
code. Often code for a single
switch  can be scattered in different places in the program. When a
new condition is added, you have to �nd all the
switch  code and modify it.
As a rule of thumb, when you see
switch  you should think of polymorphism.
Treatment
• T o isolate `switch` and put it in the right class, you may need Extract Method and then Move

 

Method.
• If a `switch` is based on type code, such as when the program’s runtime mode is switched,
use Replace T ype Code with Subclasses or Replace T ype Code with State/Strategy.
• After specifying the inheritance structure, use Replace Conditional with Polymorphism.
• If there are not too many conditions in the operator and they all call same method with
different parameters, polymorphism will be super�uous. If this case, you can break that
method into multiple smaller methods with Replace Parameter with Explicit Methods and
change the `switch` accordingly.
• If one of the conditional options is
null , use Introduce Null Object.
Payoff
Improved code organization.
When to Ignore
• When a
switch  operator performs simple actions, there is no reason to make code changes.
• Often
switch  operators are used by factory design patterns (Factory Method and Abstract
Factory) to select a created class.
Reading is boring
Aren't you bored of reading so much? Try out our new interactive

READ NEXTRETURN
T emporary Field  Object-Orientation Abusers
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
learning course on refactoring. It has more content and much more
fun.
 Learn more
