(defun c:LispAppCommand ( )

 (setq a (TestEntitlementLisp))
 
	(if (= a 1)
  		(progn
			;;;(princ 100)
    			return "success"
  		)
  		(progn
			;;;(princ 200)
    		return "Faile"
  		)
	)
)

