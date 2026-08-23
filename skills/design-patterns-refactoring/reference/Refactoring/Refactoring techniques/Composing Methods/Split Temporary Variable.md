# Split Temporary Variable

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Split Temporary Variable
Problem
You have a local variable that is used to store various intermediate values inside a method (except
for cycle variables).
Solution
Use different variables for different values. Each variable should be responsible for only one
particular thing.
Why Refactor
If you are skimping on the number of variables inside a function and reusing them for various
unrelated purposes, you are sure to encounter problems as soon as you need to make changes to
double temp = 2 * (height + width);
System.out.println(temp);
temp = height * width;
System.out.println(temp);
final double perimeter = 2 * (height + width);
System.out.println(perimeter);
final double area = height * width;
System.out.println(area);
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Split Temporary Variable
https://sourcemaking.com/refactoring/split-temporary-variable 1/3

the code containing the variables. You will have to recheck each case of variable use to make sure
that the correct values are used.
Benefits
Each component of the program code should be responsible for one and one thing only. This
makes it much easier to maintain the code, since you can easily replace any particular thing
without fear of unintended effects.
Code becomes more readable. If a variable was created long ago in a rush, it probably has a
name that does not explain anything: k, a2, value, etc. But you can fix this situation by naming
the new variables in an understandable, self-explanatory way. Such names might resemble
customerTaxValue, cityUnemploymentRate, clientSalutationString and the like.
This refactoring technique is useful if you anticipate using Extract Method later.
How to Refactor
1. Find the first place in the code where the variable is given a value. Here you should rename the
variable with a name that corresponds to the value being assigned.
2. Use the new name instead of the old one in places where this value of the variable is used.
3. Repeat as needed for places where the variable is assigned a different value.
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:10 Split Temporary Variable
https://sourcemaking.com/refactoring/split-temporary-variable 2/3

RETURN READ NEXT
Remove Assignments to Parameters    Replace Temp with Query
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:10 Split Temporary Variable
https://sourcemaking.com/refactoring/split-temporary-variable 3/3
