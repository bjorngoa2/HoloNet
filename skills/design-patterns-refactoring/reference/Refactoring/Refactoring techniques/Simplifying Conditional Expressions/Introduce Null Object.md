# Introduce Null Object

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Conditional Expressions
Introduce Null Object
Problem
Since some methods return null instead of real objects, you have many checks for null in your
code.
Solution
Instead of null, return a null object that exhibits the default behavior.
if (customer == null) {
  plan = BillingPlan.basic();
}
else {
  plan = customer.getPlan();
}
class NullCustomer extends Customer {
  boolean isNull() {
    return true;
  }
  Plan getPlan() {
    return new NullPlan();
  }
  // Some other NULL functionality.
}
// Replace null values with Null-object.
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Introduce Null Object
https://sourcemaking.com/refactoring/introduce-null-object 1/3

Why Refactor
Dozens of checks for null make your code longer and uglier.
Drawbacks
The price of getting rid of conditionals is creating yet another new class.
How to Refactor
1. From the class in question, create a subclass that will perform the role of null object.
2. In both classes, create the method isNull(), which will return true for a null object and false
for a real class.
3. Find all places where the code may return null instead of a real object. Change the code so
that it returns a null object.
4. Find all places where the variables of the real class are compared with null. Replace these
checks with a call for isNull().
5. If methods of the original class are run in these conditionals when a value does not equal
null, redefine these methods in the null class and insert the code from the else part of
the condition there. Then you can delete the entire conditional and differing behavior will be
implemented via polymorphism.
If things are not so simple and the methods cannot be redefined, see if you can simply
extract the operators that were supposed to be performed in the case of a null value to
new methods of the null object. Call these methods instead of the old code in else as the
operations by default.
customer = (order.customer != null) ?
  order.customer : new NullCustomer();
// Use Null-object as if it's normal subclass.
plan = customer.getPlan();
23.08.2026, 11:11 Introduce Null Object
https://sourcemaking.com/refactoring/introduce-null-object 2/3

RETURN READ NEXT
Introduce Assertion    Replace Conditional with Polymorphism
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
23.08.2026, 11:11 Introduce Null Object
https://sourcemaking.com/refactoring/introduce-null-object 3/3
