(define drawRandomElement (function ()
    (define type (random.pick ("Box" "Sphere" "Capsule")))
    (define position (randomVector
        {"x" -20 "y" 2 "z" -20}
        {"x" 20  "y" 14 "z" 20}
    ))
    (define scale (randomVector
        {"x" 0.5 "y" 0.5 "z" 0.5}
        {"x" 1.5 "y" 1.5 "z" 1.5}
    ))
    (draw.element type position (randomColour) scale)
))

(draw.clear)
(makeGround)
(define i 0)
(loop (< i 100)
    (drawRandomElement)
    (inc i)
)