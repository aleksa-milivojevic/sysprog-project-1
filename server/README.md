server: treba da prihvata http GET iskljucivo, svaki prebaci u zaseban thread i svaki trhead vraca response sa trazenim fajlom

api: salje get metode serveru i ima cache sa prihvacenim fajlovima, treba da implementira zastitu za cache stampede, svaki request treba u zasebni thread
zove na http://localhost:5182//fajl.txt i u konzolu pise odgovor

tolko valjda treba da se radi, ne znam da l je api odvojen projekat jer mi nema smisla da je u istom kao server