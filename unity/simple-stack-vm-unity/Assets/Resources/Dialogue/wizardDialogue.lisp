(define beginLineText (function (...input)
    (beginLine)
    (text ...input)
))

(define main (function ()
    (actor SELF)
    (beginLineText "Welcome " PLAYER.name ", I've been expecting you.")
    (endLine)

    (actor PLAYER)
    (emotion "shocked")
    (beginLineText "You have?")
    (wait 500)
    (text " How did you know I was coming?")
    (endLine)

    (:AskQuestion)
    (actor SELF)
    (beginLineText "What is it that you wish to know?")
    (choice "Do you have powers?" (function ()
        (whatPowers)
        (moveTo main :AskQuestion)
    ))
    (choice "Do you sell stuff?" (function ()
        (doYouSell)
        (moveTo main :AskQuestion)
    ))
    (choice "Chance?" (function ()
        (chance)
        (moveTo main :AskQuestion)
    ))
    (choice "Good bye" goodBye)
    (endLine)
))

(define whatPowers (function ()
    (actor "PLAYER")
    (emotion "happy")
    (beginLineText "What kind of powers do you have?")
    (endLine)
))

(define doYouSell (function ()
    (actor PLAYER)
    (beginLineText "Do you have anything to sell?")
    (endLine)

    (actor SELF)
    (emotion "sad")
    (beginLineText "Unfortunately I do not at this time, if only someone could complete this quest.")
    (choice "I can quest" (function () (jump :ICanQuest) ))
    (choice "Hope you find someone" (function () (jump :FindSomeoneElse) ))
    (endLine)

    (:ICanQuest)
    (actor PLAYER)
    (emotion "happy")
    (beginLineText "I can go on a quest for you!")
    (endLine)
    (return)

    (:FindSomeoneElse)
    (actor PLAYER)
    (emotion "sad")
    (beginLineText "Gee, well I hope you find someone.")
    (endLine)
    (return)
))

(define chance (function ()
    (actor SELF)
    (beginLineText "Are you ready for some chance?\n")
    (jump (random.pick (:Choice1 :Choice2 :Choice3)))

    (:Choice1)
    (emotion "happy")
    (text "Random choice 1")
    (jump :AfterChoices)

    (:Choice2)
    (emotion "shocked")
    (text "Random choice 2")
    (jump :AfterChoices)

    (:Choice3)
    (emotion "sad")
    (text "Random choice 3")
    (jump :AfterChoices)

    (:AfterChoices)
    (text "\nAfter random choices")
    (endLine)
))

(define goodBye (function ()
    (actor SELF)
    (beginLineText "Thanks for coming by " PLAYER.name ", see you later")
    (endLine)
))