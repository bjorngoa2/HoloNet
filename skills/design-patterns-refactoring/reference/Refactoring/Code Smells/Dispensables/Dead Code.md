# Dead Code

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Dispensables
Dead Code
Signs and Symptoms
A variable, parameter, �eld, method or class is no longer used (usually because it is obsolete).
Reasons for the Problem
When requirements for the software have changed or corrections have been made, nobody had
time to clean up the old code.
Such code could also be found in complex conditionals, when one of the branches becomes
unreachable (due to error or other circumstances).
Treatment
The quickest way to �nd dead code is to use a good IDE.
Delete unused code and unneeded �les.

 

• In the case of an unnecessary class, Inline Class or Collapse Hierarchy can be applied if a
subclass or superclass is used.
• T o remove unneeded parameters, use Remove Parameter.
Payoff
• Reduced code size.
• Simpler support.
READ NEXTRETURN
Speculative Generality  Data Class
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
