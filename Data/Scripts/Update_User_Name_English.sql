-- English Name for users whose Name is Persian.
-- FullName_En is used only when the HTS Persian full name matches Name or NameFa.
-- Other rows are transliterated. NameFa is not changed.
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE system.[User]
SET Name = N'Modir System'
WHERE Id = 1 AND Name = N'مدیر سیستم';

UPDATE system.[User]
SET Name = N'Nazmi'
WHERE Id = 211 AND Name = N'نظمی';

UPDATE system.[User]
SET Name = N'Modir System'
WHERE Id = 297 AND Name = N'مدیر سیستم';

UPDATE system.[User]
SET Name = N'Alireza Forouzesh'
WHERE Id = 301 AND Name = N'علیرضا فروزش';

UPDATE system.[User]
SET Name = N'Vahid Valizadeh'
WHERE Id = 302 AND Name = N'وحید ولی زاده';

UPDATE system.[User]
SET Name = N'Samin Nourmohammadi'
WHERE Id = 303 AND Name = N'ثمین نورمحمدی';

UPDATE system.[User]
SET Name = N'Omid Rezaei'
WHERE Id = 304 AND Name = N'امید رضائی';

UPDATE system.[User]
SET Name = N'Mohammad Alipour'
WHERE Id = 305 AND Name = N'محمد علی پور';

UPDATE system.[User]
SET Name = N'Seyedeh Tahereh Mousavi Bahambari'
WHERE Id = 306 AND Name = N'سیده طاهره موسوی بهمبری';

UPDATE system.[User]
SET Name = N'Seyedeh Fatemeh Hashemi'
WHERE Id = 307 AND Name = N'سیده فاطمه هاشمی';

UPDATE system.[User]
SET Name = N'Abbasi Mohsen'
WHERE Id = 308 AND Name = N'محسن عباسی';

UPDATE system.[User]
SET Name = N'Akbar Lotfi'
WHERE Id = 309 AND Name = N'اکبر لطفی';

UPDATE system.[User]
SET Name = N'Ali Rasouli'
WHERE Id = 310 AND Name = N'علی رسولی';

UPDATE system.[User]
SET Name = N'Soudabeh Zare Banad Kouki'
WHERE Id = 311 AND Name = N'سودابه زارع بناد کوکی';

UPDATE system.[User]
SET Name = N'Sepideh Sabaghi'
WHERE Id = 312 AND Name = N'سپیده صباغی';

UPDATE system.[User]
SET Name = N'Mahboubeh Ghasemvand'
WHERE Id = 313 AND Name = N'محبوبه قاسم وند';

UPDATE system.[User]
SET Name = N'Sepideh Rafiei'
WHERE Id = 314 AND Name = N'سپیده رفیعی';

UPDATE system.[User]
SET Name = N'Saed Khiabani'
WHERE Id = 316 AND Name = N'ساعد خیابانی';

UPDATE system.[User]
SET Name = N'Kianoush Mafi'
WHERE Id = 317 AND Name = N'کیانوش مافی';

UPDATE system.[User]
SET Name = N'Seyed Reza Mousazadeh'
WHERE Id = 318 AND Name = N'سید رضا موسی زاده';

UPDATE system.[User]
SET Name = N'Abbas Rezaei'
WHERE Id = 319 AND Name = N'عباس رضئی';

UPDATE system.[User]
SET Name = N'Farzan Nokhbeh Zaeem'
WHERE Id = 321 AND Name = N'فرزان نخبه زعیم';

UPDATE system.[User]
SET Name = N'Mostafa Ahmadi Baharvand'
WHERE Id = 322 AND Name = N'مصطفی احمدی بهاروند';

UPDATE system.[User]
SET Name = N'Alireza Habibi Ghanbarjah'
WHERE Id = 323 AND Name = N'علیرضا حبیبی قنبرجه';

UPDATE system.[User]
SET Name = N'Siavash Abdollahi'
WHERE Id = 324 AND Name = N'سیاوش عبدالهی';

UPDATE system.[User]
SET Name = N'Somayeh Khalili'
WHERE Id = 325 AND Name = N'سمیه خلیلی';

UPDATE system.[User]
SET Name = N'Alireza Afghan'
WHERE Id = 326 AND Name = N'علیرضا افغان';

UPDATE system.[User]
SET Name = N'Gholamreza Delgoshaei'
WHERE Id = 327 AND Name = N'غلامرضا دلگشائی';

UPDATE system.[User]
SET Name = N'Saeidian Azita'
WHERE Id = 328 AND Name = N'آزیتا سعیدیان';

UPDATE system.[User]
SET Name = N'Iman Bayat'
WHERE Id = 329 AND Name = N'ایمان بیات';

UPDATE system.[User]
SET Name = N'Vahid Goudarzi'
WHERE Id = 330 AND Name = N'وحید گودرزی';

UPDATE system.[User]
SET Name = N'Taghi Aghaei'
WHERE Id = 331 AND Name = N'تقی اقائی';

UPDATE system.[User]
SET Name = N'Faranak Emadi Azar'
WHERE Id = 332 AND Name = N'فرانک عمادی آذر';

UPDATE system.[User]
SET Name = N'Hamidreza Aghajani'
WHERE Id = 334 AND Name = N'حمیدرضا آقاجانی';

UPDATE system.[User]
SET Name = N'Amin Ghanbarzadeh'
WHERE Id = 336 AND Name = N'امین قنبرزاده';

UPDATE system.[User]
SET Name = N'Amir Mohsen Saadati'
WHERE Id = 338 AND Name = N'امیر محسن سعادتی';

UPDATE system.[User]
SET Name = N'Kargah Sanati Atlas'
WHERE Id = 339 AND Name = N'کارگاه صنعتی اطلس';

UPDATE system.[User]
SET Name = N'Mir Yousef Ziabari'
WHERE Id = 340 AND Name = N'میر یوسف ضیابری';

UPDATE system.[User]
SET Name = N'Shiler Kavian Pouya'
WHERE Id = 341 AND Name = N'شیلرکاویان پویا';

UPDATE system.[User]
SET Name = N'Goruh Sanati Ab'
WHERE Id = 342 AND Name = N'گروه صنعتی آب';

UPDATE system.[User]
SET Name = N'Mehdi Rostami'
WHERE Id = 344 AND Name = N'مهدی رستمی';

UPDATE system.[User]
SET Name = N'Maryam Alsadat Mousavi Lajimi'
WHERE Id = 345 AND Name = N'مریم السادات موسوی لاجیمی';

UPDATE system.[User]
SET Name = N'Farzan Teymourzadeh'
WHERE Id = 346 AND Name = N'فرزان تیمورزاده';

UPDATE system.[User]
SET Name = N'Shahrzad Farkhondeh'
WHERE Id = 347 AND Name = N'شهرزاد فرخنده';

UPDATE system.[User]
SET Name = N'Sadegh Mohammadi'
WHERE Id = 349 AND Name = N'صادق محمدی';

UPDATE system.[User]
SET Name = N'Moshaver Bostani'
WHERE Id = 350 AND Name = N'مشاور بوستانی';

UPDATE system.[User]
SET Name = N'Ahmad Nazari'
WHERE Id = 351 AND Name = N'احمد نظری';

UPDATE system.[User]
SET Name = N'Ali Mardani'
WHERE Id = 352 AND Name = N'علی مردانی';

UPDATE system.[User]
SET Name = N'Zahra Maleki'
WHERE Id = 354 AND Name = N'زهرا ملکی';

UPDATE system.[User]
SET Name = N'Reza Momeni'
WHERE Id = 355 AND Name = N'رضا مومنی';

UPDATE system.[User]
SET Name = N'Parisa Faraji'
WHERE Id = 356 AND Name = N'پریسا فرجی';

UPDATE system.[User]
SET Name = N'Alireza Sohani'
WHERE Id = 357 AND Name = N'علیرضا سوهانی';

UPDATE system.[User]
SET Name = N'Nima Nouri'
WHERE Id = 360 AND Name = N'نیما نوری';

UPDATE system.[User]
SET Name = N'Naghmeh Golkar Moghaddam'
WHERE Id = 361 AND Name = N'نغمه گل کار مقدم';

UPDATE system.[User]
SET Name = N'Ardalan Zarrineh'
WHERE Id = 363 AND Name = N'اردلان زرینه';

UPDATE system.[User]
SET Name = N'Fariborz Fouladi'
WHERE Id = 365 AND Name = N'فریبرز فولادی';

UPDATE system.[User]
SET Name = N'Marjan Afsharjam'
WHERE Id = 366 AND Name = N'مرجان افشارجم';

UPDATE system.[User]
SET Name = N'Ali Gharedaghi'
WHERE Id = 367 AND Name = N'علی قره داغی';

UPDATE system.[User]
SET Name = N'Elnaz Ardalani'
WHERE Id = 371 AND Name = N'الناز اردلانی';

UPDATE system.[User]
SET Name = N'Fariba Zare'
WHERE Id = 372 AND Name = N'فریبا زارع';

UPDATE system.[User]
SET Name = N'Mansoureh Asghari Bayati'
WHERE Id = 373 AND Name = N'منصوره اصغری بیاتی';

UPDATE system.[User]
SET Name = N'Parisa Mirza Karimi'
WHERE Id = 374 AND Name = N'پریسا میرزا کریمی';

UPDATE system.[User]
SET Name = N'Noushin Hajiyan Motlagh'
WHERE Id = 375 AND Name = N'نوشین حاجیان مطلق';

UPDATE system.[User]
SET Name = N'Ezzat Mahmoudi Yazdani'
WHERE Id = 376 AND Name = N'عزت محمودی یزدانی';

UPDATE system.[User]
SET Name = N'Ahou Yavari'
WHERE Id = 377 AND Name = N'آهو یاوری';

UPDATE system.[User]
SET Name = N'Mahsa Sakhaei'
WHERE Id = 378 AND Name = N'مهسا سخائی';

UPDATE system.[User]
SET Name = N'Fariba Hosseini Lotfabadi'
WHERE Id = 380 AND Name = N'فریبا حسینی لطف آبادی';

UPDATE system.[User]
SET Name = N'ghasem moghaddam'
WHERE Id = 381 AND Name = N'قاسم مقدم';

UPDATE system.[User]
SET Name = N'Parvaneh Eshghi'
WHERE Id = 383 AND Name = N'پروانه عشقی';

UPDATE system.[User]
SET Name = N'Elnaz Haghighi'
WHERE Id = 384 AND Name = N'الناز حقیقی';

UPDATE system.[User]
SET Name = N'Nariman Mir Tabatabaei'
WHERE Id = 385 AND Name = N'نریمان میر طباطبائی';

UPDATE system.[User]
SET Name = N'Alireza Ansar'
WHERE Id = 386 AND Name = N'علیرضا انصار';

UPDATE system.[User]
SET Name = N'Sina Mozafari'
WHERE Id = 387 AND Name = N'سینا مظفری';

UPDATE system.[User]
SET Name = N'Soheil Habib Ershadi'
WHERE Id = 388 AND Name = N'سهیل حبیب ارشادی';

UPDATE system.[User]
SET Name = N'Ali Soleimani'
WHERE Id = 389 AND Name = N'علی سلیمانی *';

UPDATE system.[User]
SET Name = N'Elaheh Sakhaei'
WHERE Id = 390 AND Name = N'الهه سخائی';

UPDATE system.[User]
SET Name = N'Mohammad Heidari Moghaddam'
WHERE Id = 391 AND Name = N'محمد حیدری مقدم';

UPDATE system.[User]
SET Name = N'Amin Morshedi Mir Agha'
WHERE Id = 392 AND Name = N'امین مرشدی میر آقا';

UPDATE system.[User]
SET Name = N'Elham Bani Asadi'
WHERE Id = 393 AND Name = N'الهام بنی اسدی';

UPDATE system.[User]
SET Name = N'Hossein Saemiyan'
WHERE Id = 394 AND Name = N'حسین صائمیان';

UPDATE system.[User]
SET Name = N'Ghazal Naderi'
WHERE Id = 395 AND Name = N'غزل نادری';

UPDATE system.[User]
SET Name = N'Mohammad Rezaei'
WHERE Id = 397 AND Name = N'محمد رضائی';

UPDATE system.[User]
SET Name = N'Namayandegi Esfahan'
WHERE Id = 398 AND Name = N'نمایندگی اصفهان';

UPDATE system.[User]
SET Name = N'Hamidreza Dehghanizadeh'
WHERE Id = 400 AND Name = N'| حمیدرضا دهقانی زاده';

UPDATE system.[User]
SET Name = N'Ebrahim Yadegari'
WHERE Id = 401 AND Name = N'ابراهیم یادگاری';

UPDATE system.[User]
SET Name = N'Mohammad Mahdavi Rad'
WHERE Id = 402 AND Name = N'محمد مهدوی راد';

UPDATE system.[User]
SET Name = N'Amir Khodamoradi'
WHERE Id = 403 AND Name = N'امیر خدامرادی';

UPDATE system.[User]
SET Name = N'Babak Shokouhmandi'
WHERE Id = 404 AND Name = N'بابک شکوهمندی';

UPDATE system.[User]
SET Name = N'Neda Nouri'
WHERE Id = 405 AND Name = N'ندا نوری';

UPDATE system.[User]
SET Name = N'Babak Nikfar'
WHERE Id = 407 AND Name = N'بابک نیک فر';

UPDATE system.[User]
SET Name = N'Saloumeh Binazir'
WHERE Id = 408 AND Name = N'سالومه بی نظیر';

UPDATE system.[User]
SET Name = N'Mahboubeh Khoddami'
WHERE Id = 409 AND Name = N'محبوبه  خدّامی';

UPDATE system.[User]
SET Name = N'Kamran Madadkhani'
WHERE Id = 410 AND Name = N'کامران مددخانی';

UPDATE system.[User]
SET Name = N'Leila Mohammadi'
WHERE Id = 411 AND Name = N'لیلا محمدی';

UPDATE system.[User]
SET Name = N'Jahedi Hamid'
WHERE Id = 413 AND Name = N'حمید جاهدی';

UPDATE system.[User]
SET Name = N'Saeed Abavisani'
WHERE Id = 414 AND Name = N'سعید ابویسانی';

UPDATE system.[User]
SET Name = N'Sara Sharafli'
WHERE Id = 416 AND Name = N'سارا شرفلی';

UPDATE system.[User]
SET Name = N'Namayeshgah'
WHERE Id = 417 AND Name = N'نمایشگاه';

UPDATE system.[User]
SET Name = N'Mahdieh Goudarzi'
WHERE Id = 418 AND Name = N'مهدیه گودرزی';

UPDATE system.[User]
SET Name = N'Behnaz Khanjani'
WHERE Id = 419 AND Name = N'بهناز خانجانی';

UPDATE system.[User]
SET Name = N'Tavakoli Fatemeh'
WHERE Id = 420 AND Name = N'فاطمه توکلی میاندهی';

UPDATE system.[User]
SET Name = N'Hasan Ahmadi'
WHERE Id = 421 AND Name = N'حسن احمدی';

UPDATE system.[User]
SET Name = N'Saghafi Farshad'
WHERE Id = 422 AND Name = N'فرشاد ثقفی';

UPDATE system.[User]
SET Name = N'Akhlaghdoust Mash''hood'
WHERE Id = 423 AND Name = N'مشهود اخلاقدوست';

UPDATE system.[User]
SET Name = N'Ali Sotoudeh Haghighi Fard'
WHERE Id = 424 AND Name = N'علی ستوده حقیقی فرد';

UPDATE system.[User]
SET Name = N'Mohammadreza Mehrabi'
WHERE Id = 425 AND Name = N'محمدرضا مهرابی';

UPDATE system.[User]
SET Name = N'Seyed Mohammad Rezvani'
WHERE Id = 426 AND Name = N'سید محمد رضوانی';

UPDATE system.[User]
SET Name = N'Alireza Golestaneh'
WHERE Id = 428 AND Name = N'علیرضا گلستانه';

UPDATE system.[User]
SET Name = N'Gilchini Hasan'
WHERE Id = 429 AND Name = N'حسن  گیل چینی';

UPDATE system.[User]
SET Name = N'Amir Mirzaei'
WHERE Id = 430 AND Name = N'امیر میرزائی';

UPDATE system.[User]
SET Name = N'Masoud Abedi'
WHERE Id = 431 AND Name = N'مسعود عابدی';

UPDATE system.[User]
SET Name = N'Shevidi Mohammad'
WHERE Id = 432 AND Name = N'محمد شویدی';

UPDATE system.[User]
SET Name = N'Valinezhad Reza'
WHERE Id = 433 AND Name = N'رضا ولی نژاد';

UPDATE system.[User]
SET Name = N'Reza Ahmadvand'
WHERE Id = 434 AND Name = N'رضا احمدوند';

UPDATE system.[User]
SET Name = N'Ali Fereydouni'
WHERE Id = 435 AND Name = N'علی فریدونی';

UPDATE system.[User]
SET Name = N'Mahdavihoseini Mohammad Reza'
WHERE Id = 438 AND Name = N'سید محمدرضا مهدوی حسینی';

UPDATE system.[User]
SET Name = N'Mehri Mohammad'
WHERE Id = 439 AND Name = N'محمد مهری';

UPDATE system.[User]
SET Name = N'Moslem Amjadi'
WHERE Id = 440 AND Name = N'مسلم امجدی';

UPDATE system.[User]
SET Name = N'Gholamreza Parsa'
WHERE Id = 441 AND Name = N'غلامرضا پارسا';

UPDATE system.[User]
SET Name = N'Farshidreza Zandi'
WHERE Id = 442 AND Name = N'فرشیدرضا زندی';

UPDATE system.[User]
SET Name = N'Mohammad Mehdi Nourbashi'
WHERE Id = 443 AND Name = N'محمد مهدی نور باشی';

UPDATE system.[User]
SET Name = N'Marjan Hadadnejad'
WHERE Id = 444 AND Name = N'مرجان حدادنژاد';

UPDATE system.[User]
SET Name = N'Masoumeh Mohammad Hosseini'
WHERE Id = 445 AND Name = N'معصومه محمد حسینی';

UPDATE system.[User]
SET Name = N'Mohammadreza Piriaei'
WHERE Id = 446 AND Name = N'محمدرضا پیریائی';

UPDATE system.[User]
SET Name = N'Mahmoud Babaei'
WHERE Id = 447 AND Name = N'محمود بابائی';

UPDATE system.[User]
SET Name = N'Alireza Bahadorkhan'
WHERE Id = 448 AND Name = N'علیرضا بهادرخان';

UPDATE system.[User]
SET Name = N'Samaneh Saadati'
WHERE Id = 449 AND Name = N'سمانه سعادتی';

UPDATE system.[User]
SET Name = N'Akbar Haghighinejad'
WHERE Id = 450 AND Name = N'اکبر حقی نژاد';

UPDATE system.[User]
SET Name = N'Mehrtash Ghahremani'
WHERE Id = 451 AND Name = N'مهرتاش قهرمانی';

UPDATE system.[User]
SET Name = N'Ali Khojasteh'
WHERE Id = 453 AND Name = N'علی خجسته';

UPDATE system.[User]
SET Name = N'Masoud Dargahi'
WHERE Id = 454 AND Name = N'مسعود درگاهی';

UPDATE system.[User]
SET Name = N'Negin Ghaffari'
WHERE Id = 455 AND Name = N'نگین غفاری';

UPDATE system.[User]
SET Name = N'Peyman Amiri'
WHERE Id = 456 AND Name = N'پیمان امیری';

UPDATE system.[User]
SET Name = N'Mohammad Amin Khorakian'
WHERE Id = 458 AND Name = N'محمد امین خوراکیان';

UPDATE system.[User]
SET Name = N'Samad Mahdlou'
WHERE Id = 459 AND Name = N'صمد مهد لو';

UPDATE system.[User]
SET Name = N'Rouhollah Omraei'
WHERE Id = 460 AND Name = N'روح الله عمرائی';

UPDATE system.[User]
SET Name = N'Asma Mousavi'
WHERE Id = 461 AND Name = N'اسماء موسوی';

UPDATE system.[User]
SET Name = N'Samsung Company'
WHERE Id = 463 AND Name = N'شرکت سامسونگ EDMS';

UPDATE system.[User]
SET Name = N'Alireza Golestaneh'
WHERE Id = 467 AND Name = N'علیرضا گلستانه';

UPDATE system.[User]
SET Name = N'Hava Atlas Maham Iranian'
WHERE Id = 468 AND Name = N'هوا اطلس مهام ایرانیان';

UPDATE system.[User]
SET Name = N'Raheleh Kazemi'
WHERE Id = 469 AND Name = N'راحله کاظمی';

UPDATE system.[User]
SET Name = N'THI Company'
WHERE Id = 471 AND Name = N'شرکت THI';

UPDATE system.[User]
SET Name = N'Arvand Sanat Bistoun'
WHERE Id = 472 AND Name = N'آروند صنعت بیستون';

UPDATE system.[User]
SET Name = N'Zahra Mehrabi'
WHERE Id = 474 AND Name = N'زهرا مهرابی';

UPDATE system.[User]
SET Name = N'Atlas Company'
WHERE Id = 475 AND Name = N'شرکت اطلس';

UPDATE system.[User]
SET Name = N'Mehdi Aref'
WHERE Id = 477 AND Name = N'مهدی عارف';

UPDATE system.[User]
SET Name = N'Mehrdad Kalantar'
WHERE Id = 480 AND Name = N'مهرداد کلانتر';

UPDATE system.[User]
SET Name = N'Abbas Hashemi'
WHERE Id = 481 AND Name = N'عباس هاشمی';

UPDATE system.[User]
SET Name = N'Kavosh Sanat Toos Company'
WHERE Id = 483 AND Name = N'شرکت کاوش صنعت توس';

UPDATE system.[User]
SET Name = N'Mahsa Tajiani'
WHERE Id = 484 AND Name = N'مهسا تاجیانی';

UPDATE system.[User]
SET Name = N'Somayeh Safari Katam Jani'
WHERE Id = 485 AND Name = N'سمیه صفری کتم جانی';

UPDATE system.[User]
SET Name = N'Hamid Reza Amini Jazi'
WHERE Id = 486 AND Name = N'حمید رضا امینی جزی';

UPDATE system.[User]
SET Name = N'Mostafa Bikdeli'
WHERE Id = 487 AND Name = N'مصطفی بیکدلی';

UPDATE system.[User]
SET Name = N'Mohammadreza Zeighami'
WHERE Id = 488 AND Name = N'محمدرضا ضیغمی';

UPDATE system.[User]
SET Name = N'Maral Pourradi Karkar'
WHERE Id = 489 AND Name = N'مارال پوررعدی کرکر';

UPDATE system.[User]
SET Name = N'Shahin Mirkhani'
WHERE Id = 490 AND Name = N'شاهین میرخانی';

UPDATE system.[User]
SET Name = N'Shadi Abedi'
WHERE Id = 491 AND Name = N'شادی عابدی';

UPDATE system.[User]
SET Name = N'Ali Alizadeh'
WHERE Id = 492 AND Name = N'علی علی زاده';

UPDATE system.[User]
SET Name = N'Zahra Goudarzi'
WHERE Id = 493 AND Name = N'زهرا گودرزی';

UPDATE system.[User]
SET Name = N'Mohsen Kashani'
WHERE Id = 494 AND Name = N'محسن کاشانی';

UPDATE system.[User]
SET Name = N'Seyedhossein Hashemi'
WHERE Id = 495 AND Name = N'سیدحسین هاشمی';

UPDATE system.[User]
SET Name = N'Iman Koucheh Baghi'
WHERE Id = 497 AND Name = N'ایمان کوچه باغی';

UPDATE system.[User]
SET Name = N'MohammadReza Ghotb'
WHERE Id = 498 AND Name = N'محمدرضا قطب';

UPDATE system.[User]
SET Name = N'Majid Fazaeli Pour Tehrani'
WHERE Id = 499 AND Name = N'مجید فضائلی پور طهرانی';

UPDATE system.[User]
SET Name = N'Abolfazl Farahani'
WHERE Id = 500 AND Name = N'ابوالفضل فراهانی';

UPDATE system.[User]
SET Name = N'Somayeh Kaboud Tabar'
WHERE Id = 501 AND Name = N'سمیه کبود تبار';

UPDATE system.[User]
SET Name = N'Mohammad Zarei'
WHERE Id = 502 AND Name = N'محمد زارعی';

UPDATE system.[User]
SET Name = N'Fatemeh Jafari'
WHERE Id = 503 AND Name = N'فاطمه جعفری';

UPDATE system.[User]
SET Name = N'Hossein Alavi'
WHERE Id = 504 AND Name = N'سید حسین علوی';

UPDATE system.[User]
SET Name = N'Mohsen Ahmadi'
WHERE Id = 505 AND Name = N'محسن احمدی';

UPDATE system.[User]
SET Name = N'Morteza Hojati'
WHERE Id = 506 AND Name = N'مرتضی حجتی';

UPDATE system.[User]
SET Name = N'Mohammad KhosroAbadi (EDMS)'
WHERE Id = 508 AND Name = N'محمد خسروآبادی';

UPDATE system.[User]
SET Name = N'Mohammadmahdi Esmaeili Moghaddam'
WHERE Id = 509 AND Name = N'محمدمهدی اسمعیلی مقدم';

UPDATE system.[User]
SET Name = N'MohammadAli Motamed'
WHERE Id = 510 AND Name = N'محمدعلی معتمد';

UPDATE system.[User]
SET Name = N'Guard Guard'
WHERE Id = 511 AND Name = N'واحد حراست';

UPDATE system.[User]
SET Name = N'Strategy Strategy'
WHERE Id = 512 AND Name = N'واحد استراتژی';

UPDATE system.[User]
SET Name = N'Naeimeh Mohammad Alipour'
WHERE Id = 513 AND Name = N'نعیمه محمد علی پور';

UPDATE system.[User]
SET Name = N'Raheleh Alsadat Ale Davoud'
WHERE Id = 514 AND Name = N'راحله السادات آل داود';

UPDATE system.[User]
SET Name = N'Mostafa Mousazadeh Kiadeh'
WHERE Id = 515 AND Name = N'مصطفی موسی زاده کیاده';

UPDATE system.[User]
SET Name = N'Behin Tajhiz Amin'
WHERE Id = 516 AND Name = N'بهین تجهیز امین';

UPDATE system.[User]
SET Name = N'Guest User'
WHERE Id = 518 AND Name = N'کاربر میهمان';

UPDATE system.[User]
SET Name = N'MohammadAmir Neshat'
WHERE Id = 519 AND Name = N'محمدامیر نشاط';

UPDATE system.[User]
SET Name = N'Omid Rouzbahani'
WHERE Id = 520 AND Name = N'امید روز بهانی';

UPDATE system.[User]
SET Name = N'Saeed Zarei'
WHERE Id = 521 AND Name = N'سعید زارعی';

UPDATE system.[User]
SET Name = N'Sahar Sobhani'
WHERE Id = 523 AND Name = N'سحر سبحانی';

UPDATE system.[User]
SET Name = N'Behzad Rafiei'
WHERE Id = 525 AND Name = N'بهزاد رفیعی';

UPDATE system.[User]
SET Name = N'Farzaneh Ghalkhani'
WHERE Id = 527 AND Name = N'فرزانه قلخانی';

UPDATE system.[User]
SET Name = N'Motahhareh Bekzad'
WHERE Id = 528 AND Name = N'مطهره بک زاد';

UPDATE system.[User]
SET Name = N'Hamidreza Moballegh'
WHERE Id = 529 AND Name = N'حمیدرضا مبلغ';

UPDATE system.[User]
SET Name = N'Masoumeh Ranjbari'
WHERE Id = 530 AND Name = N'معصومه رنجبری';

UPDATE system.[User]
SET Name = N'Mohammad Rahimi'
WHERE Id = 532 AND Name = N'محمد رحیمی';

UPDATE system.[User]
SET Name = N'Reza Kouhnavard'
WHERE Id = 533 AND Name = N'رضا کوه نورد';

UPDATE system.[User]
SET Name = N'Sajjad Shah Karami'
WHERE Id = 534 AND Name = N'سجاد شاه کرمی';

UPDATE system.[User]
SET Name = N'Somayeh Sargordan'
WHERE Id = 535 AND Name = N'سمیه سرگردان مغانلو';

UPDATE system.[User]
SET Name = N'Fahimeh KhanMohammadi'
WHERE Id = 536 AND Name = N'فهیمه خان محمدی';

UPDATE system.[User]
SET Name = N'Pouya Bidram'
WHERE Id = 537 AND Name = N'پویا بیدرام';

UPDATE system.[User]
SET Name = N'Nasrollah Najafdari'
WHERE Id = 539 AND Name = N'نصرالله نجف دری';

UPDATE system.[User]
SET Name = N'Amir Bordbar'
WHERE Id = 540 AND Name = N'امیر بردبار';

UPDATE system.[User]
SET Name = N'Sahar HamzehZadeh'
WHERE Id = 542 AND Name = N'سحر حمزه زاده';

UPDATE system.[User]
SET Name = N'Saeed Safarpour'
WHERE Id = 543 AND Name = N'سعید صفرپور';

UPDATE system.[User]
SET Name = N'Soleimani Ali'
WHERE Id = 545 AND Name = N'علی سلیمانی';

UPDATE system.[User]
SET Name = N'Shiva Askari'
WHERE Id = 548 AND Name = N'شیوا عسکری';

UPDATE system.[User]
SET Name = N'Somayeh Lotfi'
WHERE Id = 549 AND Name = N'سمیه لطفی';

UPDATE system.[User]
SET Name = N'Farshad Mostafavi'
WHERE Id = 550 AND Name = N'فرشاد مصطفوی';

UPDATE system.[User]
SET Name = N'Mohammad Moayedi'
WHERE Id = 551 AND Name = N'محمد مؤیدی';

UPDATE system.[User]
SET Name = N'Behnaz Moradi Mehr'
WHERE Id = 552 AND Name = N'بهناز مرادی مهر';

UPDATE system.[User]
SET Name = N'Shahpar Afraz'
WHERE Id = 553 AND Name = N'شهپر افراز';

UPDATE system.[User]
SET Name = N'Esmaeil Naseri'
WHERE Id = 554 AND Name = N'اسماعیل ناصری';

UPDATE system.[User]
SET Name = N'Mohammad Ghorbanlou'
WHERE Id = 557 AND Name = N'محمد قربانلو';

UPDATE system.[User]
SET Name = N'Mohammad Sedaghati'
WHERE Id = 558 AND Name = N'محمد صداقتی';

UPDATE system.[User]
SET Name = N'Ghasem Mosayebi'
WHERE Id = 559 AND Name = N'قاسم مصیبی';

UPDATE system.[User]
SET Name = N'Neda Soltan Mohammadi'
WHERE Id = 560 AND Name = N'ندا سلطان محمدی';

UPDATE system.[User]
SET Name = N'Farzad Kooravand'
WHERE Id = 561 AND Name = N'فرزاد کوراوند';

UPDATE system.[User]
SET Name = N'Shabnam Ghaffari'
WHERE Id = 562 AND Name = N'شبنم غفاری';

UPDATE system.[User]
SET Name = N'Farshid Sardabi'
WHERE Id = 563 AND Name = N'فرشید سردابی';

UPDATE system.[User]
SET Name = N'Site'
WHERE Id = 564 AND Name = N'سایت';

UPDATE system.[User]
SET Name = N'Shaghayegh Aghili'
WHERE Id = 568 AND Name = N'شقایق عقیلی';

UPDATE system.[User]
SET Name = N'Behrouz Sohrabian'
WHERE Id = 569 AND Name = N'بهروز سهرابیان';

UPDATE system.[User]
SET Name = N'Hasanali Sharifpour'
WHERE Id = 571 AND Name = N'حسنعلی شریف پور';

UPDATE system.[User]
SET Name = N'Mojgan Afshar'
WHERE Id = 573 AND Name = N'مژگان افشار';

UPDATE system.[User]
SET Name = N'Ali Raoufi Kia'
WHERE Id = 574 AND Name = N'علی رئوفی کیا';

UPDATE system.[User]
SET Name = N'Behrouz Mahdavi (HGI)'
WHERE Id = 576 AND Name = N'بهروز مهدوی';

UPDATE system.[User]
SET Name = N'Farnaz Shokranian'
WHERE Id = 578 AND Name = N'فرناز شکرانیان';

UPDATE system.[User]
SET Name = N'Reza Ghasemi'
WHERE Id = 579 AND Name = N'سیدرضا قاسمی';

UPDATE system.[User]
SET Name = N'Hamin Mehr Pardis'
WHERE Id = 580 AND Name = N'هامین مهر پردیس';

UPDATE system.[User]
SET Name = N'Varesh Sanat Koubar Caspian'
WHERE Id = 581 AND Name = N'وارش صنعت کوبار کاسپین';

UPDATE system.[User]
SET Name = N'Keyfiat Afarinan Mechanic Espadan'
WHERE Id = 582 AND Name = N'کیفیت آفرینان مکانیک اسپادان';

UPDATE system.[User]
SET Name = N'Sahand Sanat Soukht Mehrnegar'
WHERE Id = 583 AND Name = N'سهند صنعت سوخت مهرنگار';

UPDATE system.[User]
SET Name = N'Safir Sanat Soukht Alborz'
WHERE Id = 584 AND Name = N'سفیر صنعت سوخت البرز';

UPDATE system.[User]
SET Name = N'Rayan Kar Sanat Paksouz'
WHERE Id = 585 AND Name = N'رایان کار صنعت پاکسوز';

UPDATE system.[User]
SET Name = N'Takta Faham Energy Caspian'
WHERE Id = 586 AND Name = N'تکتا فهام انرژی کاسپین';

UPDATE system.[User]
SET Name = N'Tajhiz Palayesh Gas Caspian'
WHERE Id = 587 AND Name = N'تجهیز پالایش گاز کاسپین*';

UPDATE system.[User]
SET Name = N'Tara Sanat Soukht Pars'
WHERE Id = 588 AND Name = N'تارا صنعت سوخت پارس';

UPDATE system.[User]
SET Name = N'Pakyar Mechanic Bagheran Sanat'
WHERE Id = 589 AND Name = N'پاکیار مکانیک باقران صنعت';

UPDATE system.[User]
SET Name = N'Arian Sanat Ravis'
WHERE Id = 590 AND Name = N'آرین صنعت راویس';

UPDATE system.[User]
SET Name = N'Niko Sanat Azin Alborz'
WHERE Id = 591 AND Name = N'نیکو صنعت آذین البرز';

UPDATE system.[User]
SET Name = N'Azar Pishro Sanat Soukht Kimia'
WHERE Id = 592 AND Name = N'آذر پیشرو صنعت سوخت کیمیا';

UPDATE system.[User]
SET Name = N'Bita Amiri'
WHERE Id = 594 AND Name = N'بیتا امیری';

UPDATE system.[User]
SET Name = N'Ali Karami'
WHERE Id = 595 AND Name = N'علی کرمی';

UPDATE system.[User]
SET Name = N'Behran Mobadel'
WHERE Id = 597 AND Name = N'شرکت بهران مبدل';

UPDATE system.[User]
SET Name = N'Euro'
WHERE Id = 598 AND Name = N'شرکت یورو اسلات';

UPDATE system.[User]
SET Name = N'GholamReza Bami'
WHERE Id = 599 AND Name = N'غلامرضا بامی محمدی';

UPDATE system.[User]
SET Name = N'Sivan Karimi'
WHERE Id = 600 AND Name = N'سیوان کریمی';

UPDATE system.[User]
SET Name = N'Raheleh Akbari'
WHERE Id = 601 AND Name = N'راحله اکبری';

UPDATE system.[User]
SET Name = N'Hamed SheikhHasani'
WHERE Id = 602 AND Name = N'حامد شیخ حسنی';

UPDATE system.[User]
SET Name = N'Firoza EDMS Vendor'
WHERE Id = 603 AND Name = N'فیروزا';

UPDATE system.[User]
SET Name = N'Mohammad Darbansar'
WHERE Id = 604 AND Name = N'محمد دربندسر';

UPDATE system.[User]
SET Name = N'Reza Dorosti'
WHERE Id = 605 AND Name = N'رضا درستی';

UPDATE system.[User]
SET Name = N'Test Keyfiat'
WHERE Id = 606 AND Name = N'تست کیفیت';

UPDATE system.[User]
SET Name = N'Mohsen Najafzadeh'
WHERE Id = 607 AND Name = N'محسن نجف زاده';

UPDATE system.[User]
SET Name = N'Ali Dadak'
WHERE Id = 608 AND Name = N'علی دادک';

UPDATE system.[User]
SET Name = N'Narges Khayatzadeh'
WHERE Id = 609 AND Name = N'نرگس خیاط زاده';

UPDATE system.[User]
SET Name = N'Mohammad Jolaei'
WHERE Id = 610 AND Name = N'محمد جولایی';

UPDATE system.[User]
SET Name = N'Vahid Ranjbar'
WHERE Id = 611 AND Name = N'وحید رنجبر';

UPDATE system.[User]
SET Name = N'Reyhaneh Osuli'
WHERE Id = 612 AND Name = N'ریحانه اصولی';

UPDATE system.[User]
SET Name = N'Mehrdad Darvishi'
WHERE Id = 613 AND Name = N'مهرداد درویشی';

UPDATE system.[User]
SET Name = N'Raha Bakhshashi'
WHERE Id = 614 AND Name = N'رها بخششی';

UPDATE system.[User]
SET Name = N'Asadallah Houshmand'
WHERE Id = 615 AND Name = N'امیر هوشمند';

UPDATE system.[User]
SET Name = N'Elaheh Roshani'
WHERE Id = 616 AND Name = N'الهه روشنی';

UPDATE system.[User]
SET Name = N'Habib Safari'
WHERE Id = 617 AND Name = N'حبیب اله صفری';

UPDATE system.[User]
SET Name = N'Seyedhesam Hosseini'
WHERE Id = 619 AND Name = N'سیدحسام حسینی';

UPDATE system.[User]
SET Name = N'Behdad Farahmandi'
WHERE Id = 620 AND Name = N'بهداد فرحمندی';

UPDATE system.[User]
SET Name = N'Kamal Nosrati'
WHERE Id = 621 AND Name = N'کمال نصرتی';

UPDATE system.[User]
SET Name = N'Mohaddeseh Rajabalian'
WHERE Id = 622 AND Name = N'محدثه رجبعلیان';

UPDATE system.[User]
SET Name = N'Tehran Javan Co.'
WHERE Id = 623 AND Name = N'تهران جوان';

UPDATE system.[User]
SET Name = N'Ase Sanat Co.'
WHERE Id = 624 AND Name = N'آسه صنعت';

UPDATE system.[User]
SET Name = N'Hasan Rahimi'
WHERE Id = 625 AND Name = N'حسن رحیمی';

UPDATE system.[User]
SET Name = N'Mobina Bagheri'
WHERE Id = 626 AND Name = N'مبینا باقری';

UPDATE system.[User]
SET Name = N'Hadi Razifar'
WHERE Id = 630 AND Name = N'هادی رازی فر';

UPDATE system.[User]
SET Name = N'Ehsan Asgari'
WHERE Id = 631 AND Name = N'احسان عسگری';

UPDATE system.[User]
SET Name = N'Persi Iran Gas Co.'
WHERE Id = 632 AND Name = N'پرسی ایران گاز';

UPDATE system.[User]
SET Name = N'Mina Ghasemnejad'
WHERE Id = 633 AND Name = N'مینا قاسم نژاد';

UPDATE system.[User]
SET Name = N'Hamed'
WHERE Id = 634 AND Name = N'حامد تهرانی پور';

UPDATE system.[User]
SET Name = N'Dabo Sanat'
WHERE Id = 635 AND Name = N'دابو صنعت';

UPDATE system.[User]
SET Name = N'Mohammad Hossein Beiranj'
WHERE Id = 636 AND Name = N'محمد حسین بیرنج';

UPDATE system.[User]
SET Name = N'Babak Soltani'
WHERE Id = 637 AND Name = N'بابک سلطانی';

UPDATE system.[User]
SET Name = N'Zahra Hazrati'
WHERE Id = 638 AND Name = N'زهرا حضرتی';

UPDATE system.[User]
SET Name = N'Abbas Esmaeil Talaei'
WHERE Id = 639 AND Name = N'عباس اسمعیل طلائی';

UPDATE system.[User]
SET Name = N'Mehdi Mahmoudvand'
WHERE Id = 640 AND Name = N'مهدی محمودوند';

UPDATE system.[User]
SET Name = N'Afrouz Khosravi'
WHERE Id = 642 AND Name = N'افروز خسروی';

UPDATE system.[User]
SET Name = N'MohammadMahdi Chegini'
WHERE Id = 643 AND Name = N'محمدمهدی چگینی';

UPDATE system.[User]
SET Name = N'Mostafa Bidi'
WHERE Id = 644 AND Name = N'مصطفی بیدی';

UPDATE system.[User]
SET Name = N'Rozbeh YazdanBin'
WHERE Id = 646 AND Name = N'روزبه یزدان بین';

UPDATE system.[User]
SET Name = N'MohammadReza Kamal'
WHERE Id = 647 AND Name = N'محمدرضا کمال';

UPDATE system.[User]
SET Name = N'Masoumeh Rouhbakhsh'
WHERE Id = 648 AND Name = N'معصومه روح بخش';

UPDATE system.[User]
SET Name = N'Sina Seddighi'
WHERE Id = 649 AND Name = N'سیدسینا صدیقی';

UPDATE system.[User]
SET Name = N'Morteza Ghayouri'
WHERE Id = 650 AND Name = N'مرتضی غیوری';

UPDATE system.[User]
SET Name = N'Mahtab Samanipour'
WHERE Id = 651 AND Name = N'مهتاب سامانی پور';

UPDATE system.[User]
SET Name = N'Test Tolid'
WHERE Id = 652 AND Name = N'تست تولید';

UPDATE system.[User]
SET Name = N'Shahram Maghami-Kia'
WHERE Id = 653 AND Name = N'شهرام مقامی کیا';

UPDATE system.[User]
SET Name = N'Avat NikZaban'
WHERE Id = 654 AND Name = N'آوات نیک زبان';

UPDATE system.[User]
SET Name = N'Babak Teymouri'
WHERE Id = 655 AND Name = N'بابک تیموری';

UPDATE system.[User]
SET Name = N'Amrollah Moradbeigi'
WHERE Id = 659 AND Name = N'امراله مراد بیگی';

UPDATE system.[User]
SET Name = N'Seyed Hossein Mousavi'
WHERE Id = 660 AND Name = N'سید حسین موسوی';

UPDATE system.[User]
SET Name = N'Mohammadreza Bahrami'
WHERE Id = 661 AND Name = N'محمدرضا بهرامی';

UPDATE system.[User]
SET Name = N'Marzieh Khademinejad'
WHERE Id = 663 AND Name = N'مرضیه خادمی نژاد';

UPDATE system.[User]
SET Name = N'Hameh Karbaran'
WHERE Id = 666 AND Name = N'* همه کاربران';

UPDATE system.[User]
SET Name = N'Sara Arab'
WHERE Id = 667 AND Name = N'سارا عرب';

UPDATE system.[User]
SET Name = N'Parnia MohebiManesh'
WHERE Id = 668 AND Name = N'پرنیا محبی منش';

UPDATE system.[User]
SET Name = N'Farnam Hadid Bastan'
WHERE Id = 670 AND Name = N'فرنام حدید باستان';

UPDATE system.[User]
SET Name = N'Mahmoud Naghizadeh'
WHERE Id = 673 AND Name = N'محمود نقی زاده';

UPDATE system.[User]
SET Name = N'Sherkat Mohandesi Farayand Energy Atlas'
WHERE Id = 674 AND Name = N'شرکت مهندسی فرآیند انرژی اطلس';

UPDATE system.[User]
SET Name = N'Seyedeh Roghayeh Angouran Seyyedi'
WHERE Id = 676 AND Name = N'سیده رقیه انگوران سیدی';

UPDATE system.[User]
SET Name = N'Hossein Bostan Pira'
WHERE Id = 678 AND Name = N'حسین بستان پیرا';

UPDATE system.[User]
SET Name = N'Razagh Rezaei Yar Aziz'
WHERE Id = 679 AND Name = N'رزاق رضائی یار عزیز';

UPDATE system.[User]
SET Name = N'Atlas Karan Mahan Iranian'
WHERE Id = 680 AND Name = N'اطلس کاران ماهان ایرانیان';

UPDATE system.[User]
SET Name = N'Sanaz Mazaheri'
WHERE Id = 681 AND Name = N'ساناز مظاهری';

UPDATE system.[User]
SET Name = N'Mojtaba Baharinejad'
WHERE Id = 682 AND Name = N'مجتبی بهاری نژاد';

UPDATE system.[User]
SET Name = N'Mobile App User'
WHERE Id = 683 AND Name = N'* کاربر موبایل';

UPDATE system.[User]
SET Name = N'Hesabres'
WHERE Id = 684 AND Name = N'حسابرس';

UPDATE system.[User]
SET Name = N'Havayar Gas Beynolmelal'
WHERE Id = 686 AND Name = N'هوایار گاز بین الملل';

UPDATE system.[User]
SET Name = N'Mohammad Dehghan'
WHERE Id = 687 AND Name = N'محمد دهقان';

UPDATE system.[User]
SET Name = N'Hesam Salimi'
WHERE Id = 688 AND Name = N'حسام الدین سلیمی';

UPDATE system.[User]
SET Name = N'Ahmad Shahini'
WHERE Id = 689 AND Name = N'احمد شاهینی';

UPDATE system.[User]
SET Name = N'Davoud Seifabadi'
WHERE Id = 690 AND Name = N'داود سیف آبادی';

UPDATE system.[User]
SET Name = N'Armin Azodi'
WHERE Id = 691 AND Name = N'آرمین عضدی';

UPDATE system.[User]
SET Name = N'Akbar Dakhili'
WHERE Id = 692 AND Name = N'اکبر دخیلی';

UPDATE system.[User]
SET Name = N'Ahmad Sarvmeyli'
WHERE Id = 693 AND Name = N'احمد  سرومیلی';

UPDATE system.[User]
SET Name = N'Babollah Soltani'
WHERE Id = 694 AND Name = N'باب اله سلطانی';

UPDATE system.[User]
SET Name = N'Mehdi Safaei Sadr'
WHERE Id = 695 AND Name = N'مهدی  صفائی صدر';

UPDATE system.[User]
SET Name = N'Saeed Salehi'
WHERE Id = 696 AND Name = N'سعید صالحی';

UPDATE system.[User]
SET Name = N'Saeed Delshad Nasab'
WHERE Id = 697 AND Name = N'سعید دلشاد نسب';

UPDATE system.[User]
SET Name = N'Heidar Jamali'
WHERE Id = 698 AND Name = N'حیدر جمالی';

UPDATE system.[User]
SET Name = N'Babak Safavi'
WHERE Id = 699 AND Name = N'بابک صفوی';

UPDATE system.[User]
SET Name = N'Mehdi Shahmoradi'
WHERE Id = 700 AND Name = N'مهدی شاهمرادی';

UPDATE system.[User]
SET Name = N'Yazdan Arianfard'
WHERE Id = 701 AND Name = N'یزدان آرین فرد';

UPDATE system.[User]
SET Name = N'Hamidreza Farokhi'
WHERE Id = 702 AND Name = N'حمیدرضا فرخی';

UPDATE system.[User]
SET Name = N'Mostafa Allahyari'
WHERE Id = 705 AND Name = N'مصطفی اله یاری';

UPDATE system.[User]
SET Name = N'Soroush Mousavi'
WHERE Id = 706 AND Name = N'سیدسروش موسوی';

UPDATE system.[User]
SET Name = N'MohammadReza Farahmand'
WHERE Id = 707 AND Name = N'محمدرضا فرهمند';

UPDATE system.[User]
SET Name = N'Meysam Modir Rousta'
WHERE Id = 708 AND Name = N'میثم مدیر روستا';

UPDATE system.[User]
SET Name = N'Saeed Hashemi'
WHERE Id = 709 AND Name = N'سعید هاشمی';

UPDATE system.[User]
SET Name = N'Bahram Soltanian'
WHERE Id = 710 AND Name = N'بهرام سلطانیان';

UPDATE system.[User]
SET Name = N'Seifollah Rafiei'
WHERE Id = 711 AND Name = N'سیف اله رفیعی';

UPDATE system.[User]
SET Name = N'Ahmad Dehkamari'
WHERE Id = 712 AND Name = N'احمد ده کمری';

UPDATE system.[User]
SET Name = N'Tahmas Dehkamari'
WHERE Id = 713 AND Name = N'طهماس ده کمری';

UPDATE system.[User]
SET Name = N'Pouria Khanipour'
WHERE Id = 714 AND Name = N'پوریا خانی پور';

UPDATE system.[User]
SET Name = N'Davoud Ghasemnia'
WHERE Id = 715 AND Name = N'داود قاسم نیا';

UPDATE system.[User]
SET Name = N'Abbas Bakhtiari'
WHERE Id = 716 AND Name = N'عباس بختیاری';

UPDATE system.[User]
SET Name = N'Karim Motamedi'
WHERE Id = 717 AND Name = N'کریم معتمدی';

UPDATE system.[User]
SET Name = N'Masoud Yousefi'
WHERE Id = 718 AND Name = N'مسعود یوسفی';

UPDATE system.[User]
SET Name = N'Reza Pouya'
WHERE Id = 719 AND Name = N'رضا پویا';

UPDATE system.[User]
SET Name = N'Ahmad Kouchaki'
WHERE Id = 721 AND Name = N'احمد کوچکی';

UPDATE system.[User]
SET Name = N'Ata Petro Taghiz Co'
WHERE Id = 723 AND Name = N'آتا پترو تجهیز';

UPDATE system.[User]
SET Name = N'Mohammad Reza Rasekhi'
WHERE Id = 724 AND Name = N'محمدرضا راسخی';

UPDATE system.[User]
SET Name = N'Kowsar Mirzaeinejad'
WHERE Id = 726 AND Name = N'کوثر میرزائی نژاد';

UPDATE system.[User]
SET Name = N'Maryam Azaroush'
WHERE Id = 727 AND Name = N'مریم آذروش';

UPDATE system.[User]
SET Name = N'Mehdi Delbari'
WHERE Id = 728 AND Name = N'مهدی دلبری';

UPDATE system.[User]
SET Name = N'Nader Ravandi'
WHERE Id = 730 AND Name = N'نادر راوندی';

UPDATE system.[User]
SET Name = N'Nasrin Kaed Abbasi'
WHERE Id = 731 AND Name = N'نسرین کاید عباسی';

UPDATE system.[User]
SET Name = N'Mohsen Akbarizadeh'
WHERE Id = 732 AND Name = N'محسن اکبری زاده';

UPDATE system.[User]
SET Name = N'Sajjad Kord'
WHERE Id = 733 AND Name = N'سجاد کرد';

UPDATE system.[User]
SET Name = N'Reza Bahmani'
WHERE Id = 734 AND Name = N'رضا بهمنی';

UPDATE system.[User]
SET Name = N'Ali Robat Sarpooshi'
WHERE Id = 736 AND Name = N'علی رباط سرپوشی';

UPDATE system.[User]
SET Name = N'Amirhoushang Mansouri'
WHERE Id = 737 AND Name = N'امیرهوشنگ منصوری';

UPDATE system.[User]
SET Name = N'Sanaz Mostafavi'
WHERE Id = 738 AND Name = N'ساناز مصطفوی';

UPDATE system.[User]
SET Name = N'Mitra Joudaki'
WHERE Id = 739 AND Name = N'میترا جودکی';

UPDATE system.[User]
SET Name = N'Zahra Naseri'
WHERE Id = 740 AND Name = N'زهرا ناصری';

UPDATE system.[User]
SET Name = N'Saeed Ghasemi'
WHERE Id = 743 AND Name = N'سعید قاسمی';

UPDATE system.[User]
SET Name = N'Hasan Norouzi'
WHERE Id = 744 AND Name = N'حسن نوروزی';

UPDATE system.[User]
SET Name = N'Roya Ranjbaran'
WHERE Id = 745 AND Name = N'رؤیا رنجبران';

UPDATE system.[User]
SET Name = N'Meysam Shaghaghi'
WHERE Id = 746 AND Name = N'میثم شقاقی';

UPDATE system.[User]
SET Name = N'Alireza Ghasemi'
WHERE Id = 747 AND Name = N'علیرضا قاسمی جولقانی';

UPDATE system.[User]
SET Name = N'Javad Ramezani'
WHERE Id = 748 AND Name = N'جواد رمضانی';

UPDATE system.[User]
SET Name = N'MohammadMehdi Salahi'
WHERE Id = 750 AND Name = N'محمدمهدی صلاحی عز';

UPDATE system.[User]
SET Name = N'Sina Andalibi'
WHERE Id = 752 AND Name = N'سینا عندلیبی';

UPDATE system.[User]
SET Name = N'Nima Beiki'
WHERE Id = 760 AND Name = N'نیما یل علی بیکی';

UPDATE system.[User]
SET Name = N'Ehsan BosaghZadeh'
WHERE Id = 761 AND Name = N'احسان اله بساق زاده';

UPDATE system.[User]
SET Name = N'Zahra Tabatabaei'
WHERE Id = 762 AND Name = N'زهراسادات طباطبائی';

UPDATE system.[User]
SET Name = N'Namayandegi Yazd'
WHERE Id = 763 AND Name = N'نمایندگی یزد';

UPDATE system.[User]
SET Name = N'Alireza Ranjbarian'
WHERE Id = 764 AND Name = N'علیرضا رنجبریان';

UPDATE system.[User]
SET Name = N'Mohammad Bakhtiari'
WHERE Id = 766 AND Name = N'محمد بختیاری';

UPDATE system.[User]
SET Name = N'Morteza Soleimani'
WHERE Id = 767 AND Name = N'مرتضی سلیمانی';

UPDATE system.[User]
SET Name = N'Reza Gholami'
WHERE Id = 768 AND Name = N'رضا غلامی';

UPDATE system.[User]
SET Name = N'Mohammad Saber Pahlevan'
WHERE Id = 769 AND Name = N'محمدصابر پهلوان';

UPDATE system.[User]
SET Name = N'Maryam Safari'
WHERE Id = 770 AND Name = N'مریم صفری';

UPDATE system.[User]
SET Name = N'Mohammad Nikoosefat'
WHERE Id = 772 AND Name = N'محمد نیکوصفت';

UPDATE system.[User]
SET Name = N'Masoud Safarifard'
WHERE Id = 773 AND Name = N'مسعود صفری فرد';

UPDATE system.[User]
SET Name = N'AmirHossein Beigi'
WHERE Id = 774 AND Name = N'امیرحسن بیگی';

UPDATE system.[User]
SET Name = N'Peyman Farhadi'
WHERE Id = 775 AND Name = N'پیمان فرهادی';

UPDATE system.[User]
SET Name = N'Yas Falsafi'
WHERE Id = 776 AND Name = N'یاس فلسفی';

UPDATE system.[User]
SET Name = N'Maryam Mahmoudi'
WHERE Id = 777 AND Name = N'مریم محمودی';

UPDATE system.[User]
SET Name = N'Arian.ELB Co'
WHERE Id = 778 AND Name = N'الکترو برق  آرین';

UPDATE system.[User]
SET Name = N'Mohammad Joula'
WHERE Id = 779 AND Name = N'محمد ابوطالب جولا';

UPDATE system.[User]
SET Name = N'Alireza Mehrsadeghi'
WHERE Id = 780 AND Name = N'علیرضا مهرصادقی';

UPDATE system.[User]
SET Name = N'Farzad Raman'
WHERE Id = 781 AND Name = N'فرزاد رمن';

UPDATE system.[User]
SET Name = N'Delta.s Co'
WHERE Id = 782 AND Name = N'مسیر سبز دلتا';

UPDATE system.[User]
SET Name = N'Mehrab GhanatAbadi'
WHERE Id = 783 AND Name = N'سیدمحراب قنات آبادی';

UPDATE system.[User]
SET Name = N'Zeinab Sharafi'
WHERE Id = 784 AND Name = N'زینب شرفی';

UPDATE system.[User]
SET Name = N'Reza Kazemi'
WHERE Id = 785 AND Name = N'رضا کاظمی';

UPDATE system.[User]
SET Name = N'Sanaz Sarkhani.s'
WHERE Id = 787 AND Name = N'ساناز سرخانی آقاباقر';

UPDATE system.[User]
SET Name = N'Rohollah Ferasat'
WHERE Id = 789 AND Name = N'روح اله فراست کیش';

UPDATE system.[User]
SET Name = N'Omid Mahoori.o'
WHERE Id = 791 AND Name = N'امید ماهوری';

UPDATE system.[User]
SET Name = N'Rahil Paviz'
WHERE Id = 792 AND Name = N'راحیل پاویز';

UPDATE system.[User]
SET Name = N'Sadra Shahsavar'
WHERE Id = 793 AND Name = N'صدرا شهسوار';

UPDATE system.[User]
SET Name = N'mohammad Kooravand'
WHERE Id = 794 AND Name = N'محمد کوراوند';

UPDATE system.[User]
SET Name = N'Bahareh Baghalpour'
WHERE Id = 795 AND Name = N'بهاره بقال پور';

UPDATE system.[User]
SET Name = N'mohammad Hashemi'
WHERE Id = 797 AND Name = N'محمد هاشمی';

UPDATE system.[User]
SET Name = N'Hossein Goudarzi Bahar'
WHERE Id = 799 AND Name = N'حسین گودرزی بهار';

UPDATE system.[User]
SET Name = N'Majid Farahani'
WHERE Id = 800 AND Name = N'مجید فراهانی';

UPDATE system.[User]
SET Name = N'Sina Norouzi'
WHERE Id = 801 AND Name = N'سینا نوروزی';

UPDATE system.[User]
SET Name = N'Fateme Naderipour.f'
WHERE Id = 802 AND Name = N'پریناز نادری پور';

UPDATE system.[User]
SET Name = N'Saeed Shakouri'
WHERE Id = 807 AND Name = N'سعید شکوری';

UPDATE system.[User]
SET Name = N'Mousavi Ali'
WHERE Id = 808 AND Name = N'سیدعلی موسوی';

UPDATE system.[User]
SET Name = N'Mohammad Sadegh Taghavi'
WHERE Id = 809 AND Name = N'محمدصادق تقوی';

UPDATE system.[User]
SET Name = N'Pars Control Peymankar'
WHERE Id = 814 AND Name = N'پارس کنترل پیمانکار';

UPDATE system.[User]
SET Name = N'Mohammadamin Dorani'
WHERE Id = 818 AND Name = N'محمدامین درانی';

UPDATE system.[User]
SET Name = N'Arka Niroo Fateh Alborz Peymankar'
WHERE Id = 820 AND Name = N'آرکا نیرو فاتح البرز پیمانکار';

UPDATE system.[User]
SET Name = N'Milan Electronic Peymankar'
WHERE Id = 821 AND Name = N'میلان الکترونیک پیمانکار';

UPDATE system.[User]
SET Name = N'Fidar Automation Peymankar'
WHERE Id = 822 AND Name = N'فیدار اتوماسیون پیمانکار';

UPDATE system.[User]
SET Name = N'Segal Petro Niroo Peymankar'
WHERE Id = 823 AND Name = N'سگال پترو نیرو پیمانکار';

UPDATE system.[User]
SET Name = N'Tabarestan Tablo Peymankar'
WHERE Id = 824 AND Name = N'طبرستان تابلو پیمانکار';

UPDATE system.[User]
SET Name = N'Petro Aron Tajhiz Rad Peymankar'
WHERE Id = 825 AND Name = N'پترو آرون تجهیز راد پیمانکار';

UPDATE system.[User]
SET Name = N'Naseri Havakar Sanat Peymankar'
WHERE Id = 826 AND Name = N'ناصری(هواکار صنعت) پیمانکار';

UPDATE system.[User]
SET Name = N'Rahavard Saei Peymankar'
WHERE Id = 827 AND Name = N'رهاورد ساعی پیمانکار';

UPDATE system.[User]
SET Name = N'Ata Petro Tajhiz Farin Peymankar'
WHERE Id = 828 AND Name = N'آتا پترو تجهیز فرین پیمانکار';

UPDATE system.[User]
SET Name = N'Radman Peymankar'
WHERE Id = 829 AND Name = N'رادمان پیمانکار';

UPDATE system.[User]
SET Name = N'Test IT Support Peymankar'
WHERE Id = 830 AND Name = N'تست آی تی ساپورت پیمانکار';

UPDATE system.[User]
SET Name = N'Pars Control Peymankar'
WHERE Id = 831 AND Name = N'پارس کنترل پیمانکار';

UPDATE system.[User]
SET Name = N'Mashin Sazi Shomal Pirouz Peymankar'
WHERE Id = 832 AND Name = N'ماشین سازی شمال پیروز  پیمانکار';

UPDATE system.[User]
SET Name = N'Fidar Automation Peymankar'
WHERE Id = 833 AND Name = N'فیدار اتوماسیون پیمانکار';

UPDATE system.[User]
SET Name = N'Segal Petro Niro'
WHERE Id = 834 AND Name = N'سگال پترو نیرو پیمانکار';

UPDATE system.[User]
SET Name = N'Kousha Bargh Vista Peymankar'
WHERE Id = 835 AND Name = N'کوشا برق ویستا پیمانکار';

UPDATE system.[User]
SET Name = N'Sherkat Toseeh Mohandesi Kiasa Peymankar'
WHERE Id = 836 AND Name = N'شرکت توسعه مهندسی کیاسا  پیمانکار';

UPDATE system.[User]
SET Name = N'Sherkat Mehvar Sanat Hormozd Peymankar'
WHERE Id = 837 AND Name = N'شرکت محور صنعت هورمزد پیمانکار';

UPDATE system.[User]
SET Name = N'Pars Sanat Eshteal Peymankar'
WHERE Id = 838 AND Name = N'پارس صنعت اشتعال پیمانکار';

UPDATE system.[User]
SET Name = N'Tabarestan Tablo Peymankar'
WHERE Id = 839 AND Name = N'طبرستان تابلو پیمانکار';

UPDATE system.[User]
SET Name = N'Milan Electronic Peymankar'
WHERE Id = 840 AND Name = N'میلان الکترونیک پیمانکار';

UPDATE system.[User]
SET Name = N'Mashin Sazi Shomal Pirouz Peymankar'
WHERE Id = 841 AND Name = N'ماشین سازی شمال پیروز پیمانکار';
COMMIT;
