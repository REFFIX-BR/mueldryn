<?php
/**
 * Conversor de Item.xml para Item.txt (Season 6 / Season 20 / Season 21)
 */

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $season = isset($_POST['season']) ? (int)$_POST['season'] : 20;

    if (isset($_FILES['xml_file']) && $_FILES['xml_file']['error'] === UPLOAD_ERR_OK) {
        $xmlData = simplexml_load_file($_FILES['xml_file']['tmp_name']);

        if ($xmlData) {
            header('Content-Type: text/plain');
            header('Content-Disposition: attachment; filename="Item.txt"');

            echo "// ==========================================================\n";
            echo "// Item.txt gerado a partir do Item.xml (Season {$season})\n";
            echo "// ==========================================================\n\n";

            foreach ($xmlData->Section as $section) {
                $sectionIndex = (string) $section['Index'];
                $sectionName = (string) $section['Name'];
                
                echo "{$sectionIndex}\n";

                // Cabeçalhos Base
                $baseHeader = "Type\tSlot\tSkill\tWidth\tHeight\tSerial\tOption\tDrop\tName" . str_repeat(" ", 25) . "Level\t";
                
                // Classes Base vs Season 20 vs Season 21
                $classesHeaderS6 = "DW\tDK\tFE\tMG\tDL\tSU\tRF";
                $classesHeaderS20 = "DW\tDK\tFE\tMG\tDL\tSU\tRF\tGL\tRW\tSL\tGC\tWW\tML\tIK\tAL";
                $classesHeaderS21 = "DW\tDK\tFE\tMG\tDL\tSU\tRF\tGL\tRW\tSL\tGC\tWW\tML\tIK\tAL\tCR";
                if ($season === 6) {
                    $classesHeader = $classesHeaderS6;
                } elseif ($season === 21) {
                    $classesHeader = $classesHeaderS21;
                } else {
                    $classesHeader = $classesHeaderS20;
                }

                // Cabeçalhos Específicos por Seção
                if ($sectionIndex >= 0 && $sectionIndex <= 5) {
                    $specHeader = "DmgMin\tDmgMax\tAtkSpd\tDur\tMagDur\tMagDmg\tReqLvl\tReqStr\tReqDex\tReqEne\tReqVit\tReqCmd\tNone\t";
                } elseif ($sectionIndex == 6) {
                    $specHeader = "Def\tDefRate\tDur\tReqLvl\tReqStr\tReqDex\tReqEne\tReqVit\tReqCmd\tNone\t";
                } elseif ($sectionIndex >= 7 && $sectionIndex <= 11) {
                    $specHeader = "Def\tDur\tReqLvl\tReqStr\tReqDex\tReqEne\tReqVit\tReqCmd\tNone\t";
                } elseif ($sectionIndex == 12) {
                    $specHeader = "Def\tDur\tReqLvl\tReqEne\tReqStr\tReqDex\tReqCmd\tBuyMoney\t";
                } elseif ($sectionIndex == 13 || (isset($section->Item[0]['Resistance0']))) {
                    $specHeader = "Dur\tRes0\tRes1\tRes2\tRes3\tRes4\tRes5\tRes6\tNone\t";
                } elseif ($sectionIndex == 14 || (isset($section->Item[0]['Value']))) {
                    $specHeader = "Value\t";
                } elseif ($sectionIndex == 15) {
                    $specHeader = "ReqLvl\tReqEne\tBuyMoney\t";
                } else {
                    $specHeader = "";
                }

                echo "//{$baseHeader}{$specHeader}{$classesHeader}\n";

                foreach ($section->Item as $item) {
                    $row = [];
                    
                    // Valores Base
                    $row[] = str_pad((string)$item['Index'], 4);
                    $row[] = str_pad((string)$item['Slot'], 4);
                    $row[] = str_pad((string)$item['Skill'], 5);
                    $row[] = str_pad((string)$item['Width'], 5);
                    $row[] = str_pad((string)$item['Height'], 6);
                    $row[] = str_pad((string)$item['Serial'], 6);
                    $row[] = str_pad((string)$item['Option'], 6);
                    $row[] = str_pad((string)$item['Drop'], 6);
                    $row[] = str_pad('"' . (string)$item['Name'] . '"', 33);
                    $row[] = str_pad(isset($item['Level']) ? (string)$item['Level'] : "0", 5);

                    // Valores Específicos
                    if ($sectionIndex >= 0 && $sectionIndex <= 5) {
                        $row[] = str_pad((string)$item['DamageMin'], 6);
                        $row[] = str_pad((string)$item['DamageMax'], 6);
                        $row[] = str_pad((string)$item['AttackSpeed'], 6);
                        $row[] = str_pad((string)$item['Durability'], 5);
                        $row[] = str_pad(isset($item['MagicDurability']) ? (string)$item['MagicDurability'] : "0", 6);
                        $row[] = str_pad(isset($item['MagicDamageRate']) ? (string)$item['MagicDamageRate'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireLevel']) ? (string)$item['RequireLevel'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireStrength']) ? (string)$item['RequireStrength'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireDexterity']) ? (string)$item['RequireDexterity'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireEnergy']) ? (string)$item['RequireEnergy'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireVitality']) ? (string)$item['RequireVitality'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireLeadership']) ? (string)$item['RequireLeadership'] : "0", 6);
                        $row[] = str_pad(isset($item['SetOptionStatType']) ? (string)$item['SetOptionStatType'] : "0", 6);
                    } elseif ($sectionIndex == 6) {
                        $row[] = str_pad((string)$item['Defense'], 5);
                        $row[] = str_pad((string)$item['DefenseSuccessRate'], 5);
                        $row[] = str_pad((string)$item['Durability'], 5);
                        $row[] = str_pad(isset($item['RequireLevel']) ? (string)$item['RequireLevel'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireStrength']) ? (string)$item['RequireStrength'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireDexterity']) ? (string)$item['RequireDexterity'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireEnergy']) ? (string)$item['RequireEnergy'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireVitality']) ? (string)$item['RequireVitality'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireLeadership']) ? (string)$item['RequireLeadership'] : "0", 6);
                        $row[] = str_pad(isset($item['SetOptionStatType']) ? (string)$item['SetOptionStatType'] : "0", 6);
                    } elseif ($sectionIndex >= 7 && $sectionIndex <= 11) {
                        $row[] = str_pad((string)$item['Defense'], 5);
                        $row[] = str_pad((string)$item['Durability'], 5);
                        $row[] = str_pad(isset($item['RequireLevel']) ? (string)$item['RequireLevel'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireStrength']) ? (string)$item['RequireStrength'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireDexterity']) ? (string)$item['RequireDexterity'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireEnergy']) ? (string)$item['RequireEnergy'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireVitality']) ? (string)$item['RequireVitality'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireLeadership']) ? (string)$item['RequireLeadership'] : "0", 6);
                        $row[] = str_pad(isset($item['SetOptionStatType']) ? (string)$item['SetOptionStatType'] : "0", 6);
                    } elseif ($sectionIndex == 12) {
                        $row[] = str_pad(isset($item['Defense']) ? (string)$item['Defense'] : "0", 5);
                        $row[] = str_pad(isset($item['Durability']) ? (string)$item['Durability'] : "0", 5);
                        $row[] = str_pad(isset($item['RequireLevel']) ? (string)$item['RequireLevel'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireEnergy']) ? (string)$item['RequireEnergy'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireStrength']) ? (string)$item['RequireStrength'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireDexterity']) ? (string)$item['RequireDexterity'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireLeadership']) ? (string)$item['RequireLeadership'] : "0", 6);
                        $row[] = str_pad(isset($item['BuyMoney']) ? (string)$item['BuyMoney'] : "0", 8);
                    } elseif ($sectionIndex == 13 || isset($item['Resistance0'])) {
                        $row[] = str_pad(isset($item['Durability']) ? (string)$item['Durability'] : "0", 5);
                        $row[] = str_pad(isset($item['Resistance0']) ? (string)$item['Resistance0'] : "0", 4);
                        $row[] = str_pad(isset($item['Resistance1']) ? (string)$item['Resistance1'] : "0", 4);
                        $row[] = str_pad(isset($item['Resistance2']) ? (string)$item['Resistance2'] : "0", 4);
                        $row[] = str_pad(isset($item['Resistance3']) ? (string)$item['Resistance3'] : "0", 4);
                        $row[] = str_pad(isset($item['Resistance4']) ? (string)$item['Resistance4'] : "0", 4);
                        $row[] = str_pad(isset($item['Resistance5']) ? (string)$item['Resistance5'] : "0", 4);
                        $row[] = str_pad(isset($item['Resistance6']) ? (string)$item['Resistance6'] : "0", 4);
                        $row[] = str_pad(isset($item['SetOptionStatType']) ? (string)$item['SetOptionStatType'] : "0", 6);
                    } elseif ($sectionIndex == 14 || isset($item['Value'])) {
                        $row[] = str_pad(isset($item['Value']) ? (string)$item['Value'] : "0", 6);
                    } elseif ($sectionIndex == 15) {
                        $row[] = str_pad(isset($item['RequireLevel']) ? (string)$item['RequireLevel'] : "0", 6);
                        $row[] = str_pad(isset($item['RequireEnergy']) ? (string)$item['RequireEnergy'] : "0", 6);
                        $row[] = str_pad(isset($item['BuyMoney']) ? (string)$item['BuyMoney'] : "0", 8);
                    }

                    // Classes
                    $row[] = isset($item['DW']) ? (string)$item['DW'] : "0";
                    $row[] = isset($item['DK']) ? (string)$item['DK'] : "0";
                    $row[] = isset($item['FE']) ? (string)$item['FE'] : "0";
                    $row[] = isset($item['MG']) ? (string)$item['MG'] : "0";
                    $row[] = isset($item['DL']) ? (string)$item['DL'] : "0";
                    $row[] = isset($item['SU']) ? (string)$item['SU'] : "0";
                    $row[] = isset($item['RF']) ? (string)$item['RF'] : "0";

                    if ($season == 20 || $season == 21) {
                        $row[] = isset($item['GL']) ? (string)$item['GL'] : "0";
                        $row[] = isset($item['RW']) ? (string)$item['RW'] : "0";
                        $row[] = isset($item['SL']) ? (string)$item['SL'] : "0";
                        $row[] = isset($item['GC']) ? (string)$item['GC'] : "0";
                        // Algumas sources usam WW e ML (White Wizard e Lemuria Mage) 
                        // no XML da MUDevs está KM (Kundun Mephis) e LM (Lemuria Mage)
                        $row[] = isset($item['KM']) ? (string)$item['KM'] : (isset($item['WW']) ? (string)$item['WW'] : "0");
                        $row[] = isset($item['LM']) ? (string)$item['LM'] : (isset($item['ML']) ? (string)$item['ML'] : "0");
                        $row[] = isset($item['IK']) ? (string)$item['IK'] : "0";
                        $row[] = isset($item['AL']) ? (string)$item['AL'] : "0";
                    }

                    if ($season == 21) {
                        $row[] = isset($item['CR']) ? (string)$item['CR'] : (isset($item['CS']) ? (string)$item['CS'] : "0");
                    }

                    echo implode("\t", $row) . "\n";
                }
                echo "end\n\n";
            }
            exit;
        } else {
            $error = "Erro: Arquivo XML inválido ou com formato incorreto.";
        }
    } else {
            $errorCode = isset($_FILES['xml_file']['error']) ? $_FILES['xml_file']['error'] : 'Desconhecido';
            $error = "Erro no envio do arquivo. (Código do erro: {$errorCode})";
            if ($errorCode == UPLOAD_ERR_INI_SIZE) {
                $error .= "<br>O arquivo é muito grande. Aumente o <b>upload_max_filesize</b> no seu php.ini.";
            }
    }
}
?>
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <title>Conversor MUDevs - Item.xml para Item.txt</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; background-color: #f4f6f9; }
        .container { max-width: 500px; margin: 0 auto; background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .form-group { margin-bottom: 15px; }
        label { display: block; margin-bottom: 5px; font-weight: bold; }
        input[type="file"], select { width: 100%; padding: 8px; box-sizing: border-box; }
        button { width: 100%; padding: 10px; background-color: #3c8dbc; color: #fff; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; }
        button:hover { background-color: #367fa9; }
        .error { color: red; margin-bottom: 15px; }
        .btn-back {
            display: block;
            width: 100%;
            padding: 10px;
            margin-top: 10px;
            background-color: #6c757d;
            color: #fff;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
            text-align: center;
            text-decoration: none;
        }
        .btn-back:hover { background-color: #5a6268; }
    </style>
</head>
<body>

<div class="container">
    <h2>Converter Item.xml para Item.txt</h2>
    <?php if (isset($error)): ?>
        <div class="error"><?= $error ?></div>
    <?php endif; ?>

    <form action="" method="post" enctype="multipart/form-data">
        <div class="form-group">
            <label for="xml_file">Selecione o arquivo Item.xml:</label>
            <input type="file" name="xml_file" id="xml_file" accept=".xml" required>
        </div>

        <div class="form-group">
            <label for="season">Temporada (Season) de destino:</label>
            <select name="season" id="season">
                <option value="6">Season 6 (Até Rage Fighter - RF)</option>
                <option value="20" selected>Season 20 (Até Alchemist - AL)</option>
                <option value="21">Season 21 (Até Crusader - CR)</option>
            </select>
        </div>

        <button type="submit">Converter e Baixar</button>
    </form>

    <a href="/admin/configs/general" class="btn-back">Voltar ao Painel</a>
</div>

</body>
</html>
