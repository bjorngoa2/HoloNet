# Comments

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Dispensables
Comments
Signs and Symptoms
A method is �lled with explanatory comments.
Reasons for the Problem
Comments are usually created with the best of intentions, when the author realizes that his or
her code is not intuitive or obvious. In such cases, comments are like a deodorant masking the
smell of �shy code that could be improved.
The best comment is a good name for a method or class.
If you feel that a code fragment cannot be understood without comments, try to change the
code structure in a way that makes comments unnecessary.

 

Treatment
• If a comment is intended to explain a complex expression, the expression should be split
into understandable subexpressions using Extract Variable.
• If a comment explains a section of code, this section can be turned into a separate method
via Extract Method. The name of the new method can be taken from the comment text itself,
most likely.
• If a method has already been extracted, but comments are still necessary to explain what
the method does, give the method a self-explanatory name. Use Rename Method for this.
• If you need to assert rules about a state that is necessary for the system to work, use
Introduce Assertion.
Payoff
Code becomes more intuitive and obvious.
When to Ignore
Comments can sometimes be useful:
• When explaining why something is being implemented in a particular way.
• When explaining complex algorithms (when all other methods for simplifying the algorithm
have been tried and come up short).
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more

READ NEXTRETURN
Duplicate Code  Dispensables
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
