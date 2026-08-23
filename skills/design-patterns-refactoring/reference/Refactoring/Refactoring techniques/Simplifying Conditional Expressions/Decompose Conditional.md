# Decompose Conditional

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Conditional Expressions
Decompose Conditional
Problem
You have a complex conditional (if-then/else or switch).
Solution
Decompose the complicated parts of the conditional into separate methods: the condition, then
and else.
Why Refactor
if (date.before(SUMMER_START) || date.after(SUMMER_END)) {
  charge = quantity * winterRate + winterServiceCharge;
}
else {
  charge = quantity * summerRate;
}
if (isSummer(date)) {
  charge = summerCharge(quantity);
}
else {
  charge = winterCharge(quantity);
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Decompose Conditional
https://sourcemaking.com/refactoring/decompose-conditional 1/2

The longer a piece of code is, the harder it is to understand. Things become even more hard to
understand when the code is filled with conditions:
While you are busy figuring out what the code in the then block does, you forget what the
relevant condition was.
While you are busy parsing else, you forget what the code in then does.
Benefits
By extracting conditional code to clearly named methods, you make life easier for the person
who will be maintaining the code later (such as you, two months from now!).
This refactoring technique is also applicable for short expressions in conditions. The string
isSalaryDay() is much prettier and more descriptive than code for comparing dates.
How to Refactor
1. Extract the conditional to a separate method via Extract Method.
2. Repeat the process for the then and else blocks.
RETURN READ NEXT
Consolidate Conditional Expression    Simplifying Conditional Expressions
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:11 Decompose Conditional
https://sourcemaking.com/refactoring/decompose-conditional 2/2
