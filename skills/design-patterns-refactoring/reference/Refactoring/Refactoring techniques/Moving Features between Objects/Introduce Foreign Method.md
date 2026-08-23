# Introduce Foreign Method

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Moving Features between Objects
Introduce Foreign Method
Problem
A utility class does not contain the method that you need and you cannot add the method to the
class.
Solution
Add the method to a client class and pass an object of the utility class to it as an argument.
class Report {
  // ...
  void sendReport() {
    Date nextDay = new Date(previousEnd.getYear(),
      previousEnd.getMonth(), previousEnd.getDate() + 1);
    // ...
  }
}
class Report {
  // ...
  void sendReport() {
    Date newStart = nextDay(previousEnd);
    // ...
  }
  private static Date nextDay(Date arg) {
    return new Date(arg.getYear(), arg.getMonth(), arg.getDate() + 1);
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Introduce Foreign Method
https://sourcemaking.com/refactoring/introduce-foreign-method 1/3

Why Refactor
You have code that uses the data and methods of a certain class. You realize that the code will look
and work much better inside a new method in the class. But you cannot add the method to the
class because, for example, the class is located in a third-party library.
This refactoring has a big payoff when the code that you want to move to the method is repeated
several times in different places in your program.
Since you are passing an object of the utility class to the parameters of the new method, you have
access to all of its fields. Inside the method, you can do practically everything that you want, as if
the method were part of the utility class.
Benefits
Removes code duplication. If your code is repeated in several places, you can replace these code
fragments with a method call. This is better than duplication even considering that the foreign
method is located in a suboptimal place.
Drawbacks
The reasons for having the method of a utility class in a client class will not always be clear to the
person maintaing the code after you. If the method can be used in other classes, you could benefit
by creating a wrapper for the utility class and placing the method there. This is also beneficial
when there are several such utility methods. Introduce Local Extension can help with this.
How to Refactor
1. Create a new method in the client class.
2. In this method, create a parameter to which the object of the utility class will be passed. If this
object can be obtained from the client class, you do not have to create such a parameter.
  }
}
23.08.2026, 11:10 Introduce Foreign Method
https://sourcemaking.com/refactoring/introduce-foreign-method 2/3

3. Extract the relevant code fragments to this method and replace them with method calls.
4. Be sure to leave the Foreign method tag in the comments for the method along with the advice
to place this method in a utility class if such becomes possible later. This will make it easier to
understand why this method is located in this particular class for those who will be maintaining
the software in the future.
RETURN READ NEXT
Introduce Local Extension    Remove Middle Man
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
23.08.2026, 11:10 Introduce Foreign Method
https://sourcemaking.com/refactoring/introduce-foreign-method 3/3
