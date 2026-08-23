# Feature Envy

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Couplers
Feature Envy
Signs and Symptoms
A method accesses the data of another object more than its own data.
Reasons for the Problem
This smell may occur after �elds are moved to a data class. If this is the case, you may want to
move the operations on data to this class as well.
Treatment
As a basic rule, if things change at the same time, you should keep them in the same place.
Usually data and functions that use this data are changed together (although exceptions are
possible).

 

• If a method clearly should be moved to another place, use Move Method.
• If only part of a method accesses the data of another object, use Extract Method to move the
part in question.
• If a method uses functions from several other classes, �rst determine which class contains
most of the data used. Then place the method in this class along with the other data.
Alternatively, use Extract Method to split the method into several parts that can be placed in
different places in different classes.
Payoff
• Less code duplication (if the data handling code is put in a central place).
• Better code organization (methods for handling data are next to the actual data).
When to Ignore
Sometimes behavior is purposefully kept separate from the class that holds the data. The usual
advantage of this is the ability to dynamically change the behavior (see Strategy, Visitor and
other patterns).

READ NEXTRETURN
Inappropriate Intimacy  Couplers
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
