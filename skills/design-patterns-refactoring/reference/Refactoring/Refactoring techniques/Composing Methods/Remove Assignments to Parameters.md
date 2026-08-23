# Remove Assignments to Parameters

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Remove Assignments toParameters
Problem
Some value is assigned to a parameter inside method’s body.
Solution
Use a local variable instead of a parameter.
int discount(int inputVal, int quantity) {
  if (quantity > 50) {
    inputVal -= 2;
  }
  // ...
}
int discount(int inputVal, int quantity) {
  int result = inputVal;
  if (quantity > 50) {
    result -= 2;
  }
  // ...
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Remove Assignments to Parameters
https://sourcemaking.com/refactoring/remove-assignments-to-parameters 1/3

Why Refactor
The reasons for this refactoring are the same as for Split Temporary Variable, but in this case we
are dealing with a parameter, not a local variable.
First, if a parameter is passed via reference, then after the parameter value is changed inside the
method, this value is passed to the argument that requested calling this method. Very often, this
occurs accidentally and leads to unfortunate effects. Even if parameters are usually passed by value
(and not by reference) in your programming language, this coding quirk may alienate those who are
unaccustomed to it.
Second, multiple assignments of different values to a single parameter make it difficult for you to
know what data should be contained in the parameter at any particular point in time. The problem
worsens if your parameter and its contents are documented but the actual value is capable of
differing from what is expected inside the method.
Benefits
Each element of the program should be responsible for only one thing. This makes code
maintenance much easier going forward, since you can safely replace code without any side
effects.
This refactoring helps to extract «repetitive code to separate methods» (Extract Method).
How to Refactor
1. Create a local variable and assign the initial value of your parameter.
2. In all method code that follows this line, replace the parameter with your new local variable.
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:10 Remove Assignments to Parameters
https://sourcemaking.com/refactoring/remove-assignments-to-parameters 2/3

RETURN READ NEXT
Replace Method with Method Object    Split Temporary Variable
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:10 Remove Assignments to Parameters
https://sourcemaking.com/refactoring/remove-assignments-to-parameters 3/3
