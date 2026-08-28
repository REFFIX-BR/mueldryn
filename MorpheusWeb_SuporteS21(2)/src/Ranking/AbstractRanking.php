<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

abstract class AbstractRanking implements InterfaceRanking
{
    protected $_view = "rankings/default";
    protected $_configs = array();
    protected $_frequency = array("daily" => array("title" => "Daily"), "weekly" => array("title" => "Weekly"), "monthly" => array("title" => "Monthly"));
    protected $_params = array();
    protected $_query = NULL;
    public function __construct($params = array())
    {
        $this->_params = $params;
    }
    public function getParams()
    {
        return $this->_params;
    }
    public function getQuery()
    {
        return $this->_query;
    }
    public function getView()
    {
        return $this->_view;
    }
    public function isValid()
    {
        return true;
    }
    public function getConfigs()
    {
        return $this->_configs + $this->_frequency;
    }
    public function reset()
    {
    }
}

?>