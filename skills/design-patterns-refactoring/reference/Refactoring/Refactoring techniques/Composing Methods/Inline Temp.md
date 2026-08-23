# Inline Temp

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Inline Temp
Problem
You have a temporary variable that is assigned the result of a simple expression and nothing more.
Solution
Replace the references to the variable with the expression itself.
Why Refactor
Inline local variables are almost always used as part of Replace Temp with Query or to pave the
way for Extract Method.
Benefits
boolean hasDiscount(Order order) {
  double basePrice = order.basePrice();
  return basePrice > 1000;
}
boolean hasDiscount(Order order) {
  return order.basePrice() > 1000;
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Inline Temp
https://sourcemaking.com/refactoring/inline-temp 1/2

This refactoring technique offers almost no benefit in and of itself. However, if the variable is
assigned the result of a method, you can marginally improve the readability of the program by
getting rid of the unnecessary variable.
Drawbacks
Sometimes seemingly useless temps are used to cache the result of an expensive operation that is
reused several times. So before using this refactoring technique, make sure that simplicity will not
come at the cost of performance.
How to Refactor
1. Find all places that use the variable. Instead of the variable, use the expression that had been
assigned to it.
2. Delete the declaration of the variable and its assignment line.
RETURN READ NEXT
Replace Temp with Query    Extract Variable
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
23.08.2026, 11:10 Inline Temp
https://sourcemaking.com/refactoring/inline-temp 2/2
