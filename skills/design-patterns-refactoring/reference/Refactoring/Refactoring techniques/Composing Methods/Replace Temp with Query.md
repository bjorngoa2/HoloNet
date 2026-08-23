# Replace Temp with Query

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Replace Temp with Query
Problem
You place the result of an expression in a local variable for later use in your code.
Solution
Move the entire expression to a separate method and return the result from it. Query the method
instead of using a variable. Incorporate the new method in other methods, if necessary.
double calculateTotal() {
  double basePrice = quantity * itemPrice;
  if (basePrice > 1000) {
    return basePrice * 0.95;
  }
  else {
    return basePrice * 0.98;
  }
}
double calculateTotal() {
  if (basePrice() > 1000) {
    return basePrice() * 0.95;
  }
  else {
    return basePrice() * 0.98;
  }
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Replace Temp with Query
https://sourcemaking.com/refactoring/replace-temp-with-query 1/3

Why Refactor
This refactoring can lay the groundwork for applying Extract Method for a portion of a very long
method.
The same expression may sometimes be found in other methods as well, which is one reason to
consider creating a common method.
Benefits
Code readability. It is much easier to understand the purpose of the method getTax() than the
line orderPrice() * 0.2.
Slimmer code via deduplication, if the line being replaced is used in multiple methods.
Good to Know
Performance
This refactoring may prompt the question of whether this approach is liable to cause a
performance hit. The honest answer is: yes, it is, since the resulting code may be burdened by
querying a new method. But with today’s fast CPUs and excellent compilers, the burden will almost
always be minimal. By contrast, readable code and the ability to reuse this method in other places
in program code – thanks to this refactoring approach – are very noticeable benefits.
Nonetheless, if your temp variable is used to cache the result of a truly time-consuming expression,
you may want to stop this refactoring after extracting the expression to a new method.
How to Refactor
1. Make sure that a value is assigned to the variable once and only once within the method. If not,
use Split Temporary Variable to ensure that the variable will be used only to store the result of
double basePrice() {
  return quantity * itemPrice;
}
23.08.2026, 11:10 Replace Temp with Query
https://sourcemaking.com/refactoring/replace-temp-with-query 2/3

your expression.
2. Use Extract Method to place the expression of interest in a new method. Make sure that this
method only returns a value and does not change the state of the object. If the method affects
the visible state of the object, use Separate Query from Modifier.
3. Replace the variable with a query to your new method.
RETURN READ NEXT
Split Temporary Variable    Inline Temp
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
23.08.2026, 11:10 Replace Temp with Query
https://sourcemaking.com/refactoring/replace-temp-with-query 3/3
