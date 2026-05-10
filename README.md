##Zadatak 30:

Kreirati Web server koji vrši određivanje prosečne dužine reči u okviru fajla. Svi zahtevi serveru
se šalju preko browser-a korišćenjem GET metode. U zahtevu se kao parametar navodi naziv fajla.
Server prihvata zahtev, pretražuje root folder i sve njegove podfoldere za zahtevani fajl i vrši
izračunavanje. Ukoliko traženi fajl ne postoji, vratiti grešku korisniku. Takođe, ukoliko je reč o
praznom fajlu, vratiti odgovarajuću poruku korisniku.