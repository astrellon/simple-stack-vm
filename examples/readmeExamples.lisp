(define example1 (function()
    (push "Hello")
    (push " There")
    (get "print")
    (call 2)
))

(define labelExample (function ()
    (define main (function ()
        (set x 0)
        (:start)

        (inc x)
        (if (< x 10)
            (
                (print "Less than 10: " x)
                (jump :start)
            )
        )
        (done)
    ))

    (main)
))

(define variableExample (function ()
    (define name "Global")
    (define main (function ()
        (print "Started main")
        (print name)

        (set name "Set from scope")
        (print name)

        (define name "Created in scope")
        (print name)
        (print "End main")
    ))

    (print name)
    (main)
    (print name)
))

(define ifExample1 (function ()
    (define logCounter (function ()
        (if (< counter 10)
            (print "Counter less than 10")
            (print "Counter more than 10")
        )
    ))

    (define counter 0)
    (logCounter) ; Prints Counter less than 10

    (define counter 20)
    (logCounter) ; Prints Counter more than 10
))

(define ifExample2 (function ()
    (define progress 0)
    (if (< progress 100)
        (
            (print "Still in progress")
            (print "Please wait...")
        )
        (
            (print "100% Progress")
            (print "All done")
        )
    )
))

(define unlessExample (function ()
    (define progress 0)
    (unless (< progress 100)
        (
            (print "100% Progress")
            (print "All done")
        )
        (
            (print "Still in progress")
            (print "Please wait...")
        )
    )
))

(define functionExample (function ()
    (define clamp (function (input lower upper)
        (if (< input lower)
            (return lower)
        )
        (if (> input upper)
            (return upper)
        )
        (return input)
    ))

    (print "Clamped 5 "  (clamp 5 -1 1))  ; Clamped 5 1
    (print "Clamped -5 " (clamp -5 -1 1)) ; Clamped -5 -1
    (print "Clamped 0 "  (clamp 0 -1 1))  ; Clamped 0 0
))

(define functionUnpackExample (function ()
    (define log (function (type ...inputs)
        (print "[" type "]: " ...inputs)
    ))

    (define findMin (function (...values)
        (if (== values.length 0)
            (return null)
        )

        (define min values.0)
        (define i 1)
        (loop (< i values.length)
            (define curr (array.get values i))
            (if (> min curr)
                (set min curr)
            )
            (inc i)
        )

        (return min)
    ))

    (log "Info" "Minimum Number: " (findMin 1 2 3))
    (log "Info" "Minimum Number: " (findMin 20 30 10))
    (log "Info" "Minimum Lexical: " (findMin "ABC" "DEF" "ZXC"))
    (log "Info" "Minimum Empty: " (findMin))
))

(functionUnpackExample)

(define loopExample1 (function ()
    (define i 0)
    (loop (< i 4)
        (print i)
        (inc i)
    )
    (print "Done")
))

(define loopExample2 (function ()
    (define i 0)
    (:LoopStart)
    (if (< i 4)
        (
        (print i)
            (inc i)
            (jump :LoopStart)
        )
    )
    (:LoopEnd)
    (print "Done")
))

(define continueExample (function ()
    (define i 0)
    (loop (< i 6)
        (inc i)

        (if (<= i 3)
            (continue)
        )
        (print i)
    )
    (print "Done")
))

(define breakExample (function ()
    (define i 0)
    (loop (< i 6)
        (inc i)

        (print i)

        (if (> i 3)
            (break)
        )
    )
    (print "Done")
))

(define incExample (function ()
    (define i 0)
    (print i) ; 0
    (inc i)
    (print i) ; 1

    ; Equivalent to:
    (define i 0)
    (print i) ; 0
    (set i (+ i 1))
    (print 1) ; 0
))

(define decExample (function ()
    (define i 0)
    (print i) ; 0
    (dec i)
    (print i) ; -1

    ; Equivalent to:
    (define i 0)
    (print i) ; 0
    (set i (- i 1))
    (print i) ; -1
))

(decExample)