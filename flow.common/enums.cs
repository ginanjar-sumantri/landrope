using System;

namespace flow.common
{
    public enum ToDoType
    {
        Proc_Pengukuran = 1,
        Proc_Non_BPN = 2,
        Proc_BPN = 3,
        Payment_Land = 4,
        Payment_Land_Lunas = 5,
        Payment_Land_NoLunas = 6,
        Deal_Payment = 7,
        Payment_Tax_NonPBB_Val = 8,
        Payment_Tax_NonPBB_NonVal = 9,
        Payment_Tax_PBB = 10,
        Payment_Process = 11,
        Land_Approval = 12,
        Land_Approval_Bebas = 13,
        Land_Approval_BelumBebas = 14
    }

    public enum ToDoState
    {
        unknown_ = 0,
        created_ = 1,
        issued_ = 2,
        delegated_ = 3,
        bundling_ = 4,
        bundled_ = 5,
        analysing_ = 7,
        canceled_ = 8,
        analisysReady_ = 9,
        analised_ = 10,
        formFilling_ = 11,
        bundleTaken_ = 12,
        accepted_ = 13,
        sentToAdmin_ = 14,
        adminCompleting_ = 15,
        resultArchiving_ = 16,
        resultArchived_ = 17,
        resultValidated_ = 18,
        allDone_ = 19,
        complished_ = 20,
        confirming_ = 21,
        cancelling_ = 22,
        postponed_ = 23,
        bestowed_ = 24,
        archiveVerified_ = 25,
        archiveApproved_ = 26,
        archiveRejected_ = 27,
        adminReceived_ = 28,
        spsPaid_ = 29,

        submissionSubmitted_ = 41,
        reviewApproved_ = 42,
        accountingApproved_ = 43,
        finalApproved_ = 44,
        aborted_ = 45,
        finalAborted_ = 46,

        continued_ = 47,
        reviewerApproval_ = 48,
        accountingApproval_ = 49,
        reviewerApproved_ = 50,
        reviewAndAcctgApproved_ = 51,
        cashierApproved_ = 52,
        rejected_ = 53,

        approvalIssued_ = 54,
        auditApproval_ = 55,
        auditApproved_ = 56,
        mapReviewed_ = 57,
        onHold_ = 58,
        mapAnalyst_ = 59,
        documentUpdate_ = 60,

        validation_ = 61,
        verification1_ = 62,
        verification2_ = 63,
        verification3_ = 64,
        verifLegal_ = 65,

        dpPaid_ = 66,
        lunasPaid_ = 67,
        processClose_ = 68,
        taxClose_ = 69,
        landApproved1_ = 70,
        landRejected2_ = 71,
        landApproved2_ = 72,
        landApproved3_ = 73,
        landApproved4_ = 74,
        landApproved5_ = 75
    }

    public enum ToDoVerb
    {
        unknown_ = 0,
        create_ = 1,
        issue_ = 2,
        delegate_ = 3,
        bundle_ = 4,
        bundleComplete_ = 5,
        analyse_ = 6,
        analyseResult_ = 7,
        analyseEntry_ = 8,
        formFill_ = 9,
        giveBundle_ = 10,
        accept_ = 11,
        sendToAdmin_ = 12,
        adminCompletion_ = 13,
        sendtoArchive_ = 14,
        archiveReceive_ = 15,
        archiveValidate_ = 16,
        confirm_ = 17,
        cancelConfirm_ = 18,
        bestow_ = 19,
        archiveVerify_ = 20,
        archiveApproval_ = 21,
        adminReceive_ = 22,
        paySPS_ = 23,

        submissionSubmit_ = 41,
        reviewApproval_ = 42,
        accountingApproval_ = 43,
        cashierApproval_ = 44,
        paymentSubmit_ = 45,
        abort_ = 46,
        confirmAbort_ = 47,

        continue_ = 48,
        approve_ = 49,
        reissue_ = 50,
        cashierApprove_ = 51,
        recipeReceive_ = 52,
        approvalIssue_ = 53,
        mapReview_ = 54,
        hold_ = 55,
        inputDocument_ = 56,
        fullPayment_ = 57,

        dpPay_ = 58,
        lunasPay_ = 59,
        uploadDocument_ = 60,
        complish_ = 61,
        approveDp_ = 62,

        landApprove1_ = 63,
        landApprove2_ = 64,
        landReject1_ = 65,
        landReject2_ = 66,
        landAbort1_ = 67,
        landAbort2_ = 68,

        utjPay_ = 69,

        landApprove3_ = 70,
        landApprove4_ = 71,
        landApprove5_ = 72,
        landApprove6_ = 73,
        landReject3_ = 74,
        landReject4_ = 75
    }

    public enum ToDoControl
    {
        _ = 0,
        ok_ = 1,
        yes_ = 2,
        continue_ = 3,
        accept_ = 4,
        approve_ = 5,
        resume_ = 6,
        done_ = 7,
        anyTrue_ = 99,

        cancel_ = 101,
        no_ = 102,
        abort_ = 103,
        reject_ = 104,
        discard_ = 105,
        pend_ = 106,
        hold_ = 107,
        postpone_ = 108,
        anyFalse_ = 199
    }

    public enum FlowModul
    {
        Unknown = 0,
        Assigment = 1,
        Bayar = 2,
        Proses = 3,
        Pradeal = 4,
        LandApprove = 5
    }

    public static class ToDoControlExtensions
    {
        public static FlowModul toEnumModul(this int modul) =>
            modul switch
            {
                1 => FlowModul.Assigment,
                2 => FlowModul.Bayar,
                3 => FlowModul.Proses,
                4 => FlowModul.Pradeal,
                5 => FlowModul.LandApprove,
                _ => FlowModul.Unknown
            };
        
        public static string Caption(this ToDoControl control) =>
            control switch
            {
                ToDoControl.abort_ => "Gugurkan",
                ToDoControl.accept_ => "Terima",
                ToDoControl.anyFalse_ => "Salah",
                ToDoControl.anyTrue_ => "Benar",
                ToDoControl.approve_ => "Setujui",
                ToDoControl.cancel_ => "Batal",
                ToDoControl.discard_ => "Buang",
                ToDoControl.done_ => "Sudah",
                ToDoControl.hold_ => "Tahan",
                ToDoControl.no_ => "Tidak",
                ToDoControl.ok_ => "OK",
                ToDoControl.pend_ => "Tunda",
                ToDoControl.reject_ => "Tolak",
                ToDoControl.resume_ => "Teruskan",
                ToDoControl.yes_ => "Ya",
                ToDoControl.postpone_ => "Tunda",
                _ => "Lanjutkan" // continue_
            };

        public static string Name(this ToDoType type) =>
            type switch
            {
                ToDoType.Proc_Pengukuran => "PENGUKURAN",
                ToDoType.Proc_BPN => "PROSESBPN",
                ToDoType.Proc_Non_BPN => "PROSESNOT",
                ToDoType.Payment_Land => "PAYMENT_LAND",
                ToDoType.Payment_Land_Lunas => "PAYMENT_LAND_LUNAS",
                ToDoType.Payment_Land_NoLunas => "PAYMENT_LAND_NOLUNAS",
                ToDoType.Deal_Payment => "DEAL_PAYMENT",
                ToDoType.Payment_Process => "PAYMENT_PROCESS",
                ToDoType.Payment_Tax_NonPBB_Val => "PAYMENT_TAX_NONPBB_VAL",
                ToDoType.Payment_Tax_NonPBB_NonVal => "PAYMENT_TAX_NONPBB_NONVAL",
                ToDoType.Payment_Tax_PBB => "PAYMENT_TAX_PBB",
                ToDoType.Land_Approval => "LAND_APPROVAL",
                ToDoType.Land_Approval_Bebas => "LAND_APPROVAL_BEBAS",
                ToDoType.Land_Approval_BelumBebas => "LAND_APPROVAL_BELUMBEBAS",
                _ => ""
            };

        public static string Title(this ToDoVerb verb)
            => verb switch
            {
                ToDoVerb.accept_ => "Terima Penugasan|Terima Penugasan Ini dan Mulai Proses",
                ToDoVerb.issue_ => "Terbitkan|Terbitkan ini sekarang",
                ToDoVerb.delegate_ => "Pilih PIC|Percayakan Tugas Ini",
                ToDoVerb.bundleComplete_ => "Bundle Siap|Bundle Penugasan Telah Siap",
                ToDoVerb.giveBundle_ => "Serahkan Bundle|Bundle Telah Diserahkan ke PIC",
                ToDoVerb.formFill_ => "Pengisian Form BPN|Mulai Mengisi Form BPN Sekarang",
                ToDoVerb.archiveReceive_ => "Penerimaan Arsip|Konfirmasi Penerimaan Dokumen",
                ToDoVerb.archiveValidate_ => "Validasi Arsip|Validasi Dokumen Diterima",
                ToDoVerb.sendToAdmin_ => "Serahkan ke Admin|Serahkan Dokumen Hasil ke Admin",
                ToDoVerb.sendtoArchive_ => "Serahkan ke Arsip|Kirim Dokumen Hasil ke Arsip",
                ToDoVerb.adminCompletion_ => "Lengkapi Data|Mulai Melengkapi Data Hasil Sekarang",
                ToDoVerb.analyseEntry_ => "Tentukan Hasil GPS|Bidang Masuk dalam SK",
                ToDoVerb.analyseResult_ => "Hasil GPS Siap|Hasil GPS Sudah Diserahkan",
                ToDoVerb.analyse_ => "Penelusuran GPS|Mulai Menelusuri dengan GPS",
                ToDoVerb.bundle_ => "Pembundelan|Buat Bundle Penugasan Sekarang",
                ToDoVerb.confirm_ => "Konfirmasi Pembebasan|Tetapkan bidang ini sebagai 'Bebas'",
                ToDoVerb.cancelConfirm_ => "Konfirmasi Pembatalan|Batalkan bidang Ini",
                ToDoVerb.bestow_ => "Limpahkan ke PIC|Limpahkan Penugasan ini ke PIC",
                ToDoVerb.paySPS_ => "Terima SPS|Simpan informasi SPS sekarang",
                ToDoVerb.submissionSubmit_ => "Terima Bukti Bayar|Sudah terima Bukti Bayar",
                ToDoVerb.reviewApproval_ or ToDoVerb.accountingApproval_ or ToDoVerb.cashierApproval_ => "Persetujuan|Setujui Permintaan Ini",
                ToDoVerb.continue_ => "Lanjutkan Proses|Lanjutkan proses permintaan ini",
                ToDoVerb.approve_ => "Teruskan|Teruskan permintaan ini",
                ToDoVerb.reissue_ => "Terbitkan kembali|Terbitkan kembali permintaan ini",
                ToDoVerb.cashierApprove_ => "Setujui|Setujui permintaan ini",
                ToDoVerb.recipeReceive_ => "Terima Bukti Bayar|Sudah terima bukti bayar",
                ToDoVerb.approvalIssue_ => "Terbitkan Penugasan",
                ToDoVerb.abort_ => "Batalkan",
                ToDoVerb.confirmAbort_ => "Konfirmasi Batal",
                ToDoVerb.hold_ => "Hold Request",
                ToDoVerb.inputDocument_ => "Masukkan Dokumen",
                ToDoVerb.fullPayment_ => "Pelunasan",
                ToDoVerb.utjPay_ => "Ajukan Pembayaran UTJ",
                ToDoVerb.dpPay_ => "Ajukan Pembayaran DP",
                ToDoVerb.lunasPay_ => "Ajukan Pelunasan",
                ToDoVerb.approveDp_ => "Approve Pengajuan Pembayaran DP",
                ToDoVerb.complish_ => "Approve Pengajuan Pelunasan",
                ToDoVerb.mapReview_ => "Lanjutkan Proses",
                ToDoVerb.uploadDocument_ => "Upload Document",
                ToDoVerb.landApprove1_ or ToDoVerb.landApprove2_ or ToDoVerb.landApprove3_ or ToDoVerb.landApprove4_ or ToDoVerb.landApprove5_ or ToDoVerb.landApprove6_ => "Approve",
                ToDoVerb.landReject1_ or ToDoVerb.landReject2_ or ToDoVerb.landReject3_ or ToDoVerb.landReject4_ => "Reject",
                ToDoVerb.landAbort1_ => "Batalkan",
                ToDoVerb.landAbort2_ => "Batalkan",
                ToDoVerb.create_ => "Dibuat",
                _ => ""
            };

        public static string TitleX(this ToDoVerb verb)
            => verb switch
            {
                ToDoVerb.sendtoArchive_ => "Serahkan ke Arsip|Kirim Dokumen ke Arsip",
                ToDoVerb.archiveReceive_ => "Penerimaan Arsip|Konfirmasi Penerimaan Dokumen",
                ToDoVerb.archiveValidate_ => "Validasi Arsip|Validasi Dokumen Diterima",
                ToDoVerb.archiveVerify_ => "Verifikasi oleh Arsip|Verifikasi Peminjaman Dokumen",
                ToDoVerb.archiveApproval_ => "Persetujuan oleh Arsip|Persetujuan Peminjaman Dokumen",
                _ => ""
            };

        public static string TitleR(this ToDoVerb verb)
             => verb switch
             {
                 ToDoVerb.reissue_ => "Terbitkan kembali|Terbitkan kembali permintaan ini",
                 ToDoVerb.landApprove1_ or ToDoVerb.landApprove2_ or ToDoVerb.landApprove3_ or ToDoVerb.landApprove4_ or ToDoVerb.landApprove5_ or ToDoVerb.landApprove6_ => "Approve",
                 ToDoVerb.landReject1_ or ToDoVerb.landReject2_ or ToDoVerb.landReject3_ or ToDoVerb.landReject4_ => "Reject",
                 ToDoVerb.landAbort1_ or ToDoVerb.landAbort2_ => "Batalkan",
                 ToDoVerb.complish_ => "Approve Request",
                 _ => ""
             };

        public static string AsStatus(this ToDoState state) => state switch
        {
            ToDoState.created_ => "Baru Dibuat",
            ToDoState.issued_ => "Diterbitkan",
            ToDoState.delegated_ => "Didelegasikan",
            ToDoState.bundling_ => "Persiapan Bundle",
            ToDoState.bundled_ => "Bundle Siap",
            ToDoState.analysing_ => "Sedang Analisa GPS",
            ToDoState.canceled_ => "Dibatalkan",
            ToDoState.analisysReady_ => "Hasil Analisa Siap",
            ToDoState.analised_ => "Sukses Dianalisa",
            ToDoState.formFilling_ => "Sedang Pengisian Form BPN",
            ToDoState.bundleTaken_ => "Bundle Sudah Diterima",
            ToDoState.accepted_ => "Penugasan Diterima",
            ToDoState.sentToAdmin_ => "Hasil Terkirim ke Admin",
            ToDoState.adminCompleting_ => "Penyelesaian di Admin",
            ToDoState.resultArchiving_ => "Pengarsipan Hasil",
            ToDoState.resultArchived_ => "Hasil Diterima Arsip",
            ToDoState.resultValidated_ => "Hasil Telah Divalidasi",
            ToDoState.allDone_ => "Selesai Seluruhnya",
            ToDoState.complished_ => "Tuntas",
            ToDoState.confirming_ => "Mengkonfirmasi Pembebasan",
            ToDoState.cancelling_ => "Mengkonfirmasi Pembatalan",
            ToDoState.postponed_ => "Ditangguhkan",
            ToDoState.bestowed_ => "Dilimpahkan ke PIC",
            ToDoState.spsPaid_ => "SPS Sudah Diterima",
            ToDoState.reviewApproved_ => "Disetujui GM",
            ToDoState.accountingApproved_ => "Disetujui Accounting",

            ToDoState.continued_ => "Proses Dilanjutkan",
            ToDoState.reviewerApproval_ => "Persetujuan Atasan",
            ToDoState.accountingApproval_ => "Persetujuan Accounting",
            ToDoState.reviewerApproved_ => "Disetujui Atasan",
            ToDoState.reviewAndAcctgApproved_ => "Sudah Disetujui Atasan dan Accounting",
            ToDoState.cashierApproved_ => "Disetujui Kasir",
            ToDoState.rejected_ => "Perlu Perbaikan",
            ToDoState.aborted_ => "Konfirmasi Pembatalan",
            ToDoState.finalAborted_ => "Telah dibatalkan",
            ToDoState.auditApproval_ => "Persetujuan Audit",
            ToDoState.mapAnalyst_ => "Review By Analyst",
            ToDoState.documentUpdate_ => "Update Document",
            ToDoState.mapReviewed_ => "Telah di Review Tim Analyst",
            ToDoState.validation_ => "Sedang divalidasi",
            ToDoState.verification1_ => "Verifikasi 1",
            ToDoState.verification2_ => "Verifikasi 2",
            ToDoState.verification3_ => "Verifikasi 3",
            ToDoState.verifLegal_ => "Verifikasi FA Legal",
            ToDoState.processClose_ => "Telah terima dari Kasir",
            ToDoState.taxClose_ => "Telah terima dari Kasir",
            ToDoState.approvalIssued_ => "Persetujuan Dikeluarkan",
            ToDoState.archiveVerified_ => "",
            ToDoState.archiveApproved_ => "",
            ToDoState.archiveRejected_ => "",
            ToDoState.adminReceived_ => "",
            ToDoState.submissionSubmitted_ => "",
            ToDoState.finalApproved_ => "",
            ToDoState.auditApproved_ => "",
            ToDoState.onHold_ => "",
            ToDoState.dpPaid_ => "Approval SPK DP",
            ToDoState.lunasPaid_ => "Approval SPK Pelunasan",
            _ => "",
        };

        public static string AsStatusX(this ToDoState state) => state switch
        {
            ToDoState.created_ => "Baru Dibuat",
            ToDoState.canceled_ => "Peminjaman Ditolak",
            ToDoState.accepted_ => "Peminjaman Diterima",
            ToDoState.sentToAdmin_ => "Dokumen Terkirim ke Admin",
            ToDoState.resultArchiving_ => "Pengarsipan dokumen",
            ToDoState.resultArchived_ => "Dokumen Diterima Arsip",
            ToDoState.resultValidated_ => "Dokumen Tervalidasi di Arsip",
            ToDoState.confirming_ => "Dokumen diterima Admin",
            _ => "",
        };

        public static string AsLandApprovalStatus(this ToDoState state) => state switch
        {
            ToDoState.created_ => "Approval Atasan",
            ToDoState.issued_ => "Approval Atasan",
            ToDoState.rejected_ => "Perlu Perbaikan",
            ToDoState.landApproved1_ => "Approval Tim Pra Pembebasan|Approval Tim Pembayaran",
            ToDoState.landApproved2_ => "Approval GM LA|Approval Tim Pra Pembebasan",
            ToDoState.landApproved3_ => "Approval GM LA",
            ToDoState.landApproved4_ => "Konfirmasi Accounting",
            ToDoState.landApproved5_ => "Konfirmasi Cashier",
            ToDoState.landRejected2_ => "Ditolak Approval Tingkat 2",
            ToDoState.complished_ => "Tuntas",
            _ => "",
        };
    }
}